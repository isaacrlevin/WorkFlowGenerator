using System;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using WorkFlowGenerator.Exceptions;
using WorkFlowGenerator.Models;
using WorkFlowGenerator.Services;
using WorkFlowGenerator.Templates;

namespace WorkFlowGenerator;

internal class Program
{
    private readonly IFileSystem _fileSystem;
    private readonly IAzureService _azureService;
    private readonly IReporter _reporter;
    private readonly IProjectDiscoveryService _projectDiscoveryService;
    private readonly IRepoService _repoService;
    private readonly IGitHubService _gitHubService;
    private static IConfiguration Configuration { get; set; }

    private WorkflowSettings WorkflowSettings { get; set; }

    [Argument(0, Description = "The path to a .csproj or .fsproj file, or to a directory containing a .NET Core solution/project. " +
                       "If none is specified, the current directory will be used.")]
    public string Path { get; set; }

    static IHostBuilder CreateDefaultBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(app =>
            {
                app.AddJsonFile("appsettings.json");
                app.AddJsonFile("appsettings.Development.json");
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<IConsole>(PhysicalConsole.Singleton);
                services.AddSingleton<IReporter>(provider => new ConsoleReporter(provider.GetService<IConsole>()));
                services.AddSingleton<IFileSystem, FileSystem>();
                services.AddSingleton<IProjectDiscoveryService, ProjectDiscoveryService>();
                services.AddSingleton<IRepoService, RepoService>();
                services.AddSingleton<IGitHubService, GitHubService>();
                services.AddSingleton<IAzureService, AzureService>();
            }); ;
    }

    public static int Main(string[] args)
    {
        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true, true)
            .AddJsonFile("appsettings.Development.json", true, true)
            .Build();

        using (
            var services = new ServiceCollection()
                .AddSingleton<IConsole>(PhysicalConsole.Singleton)
                .AddSingleton<IReporter>(provider => new ConsoleReporter(provider.GetService<IConsole>()))
                .AddSingleton<IFileSystem, FileSystem>()
                .AddSingleton<IProjectDiscoveryService, ProjectDiscoveryService>()
                .AddSingleton<IRepoService, RepoService>()
                .AddSingleton<IGitHubService, GitHubService>()
                .AddSingleton<IAzureService, AzureService>()
                .Configure<AppSettings>(Configuration)
                .BuildServiceProvider())
        {
            var app = new CommandLineApplication<Program>();
            app.Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(services);

            return app.Execute(args);
        }
    }

    public static string GetVersion() => typeof(Program)
.Assembly
.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
.InformationalVersion;

    public Program(IFileSystem fileSystem, IReporter reporter, IProjectDiscoveryService projectDiscoveryService, IRepoService repoService, IAzureService azureService, IGitHubService gitHubService)
    {
        _gitHubService = gitHubService;
        _azureService = azureService;
        _fileSystem = fileSystem;
        _reporter = reporter;
        _projectDiscoveryService = projectDiscoveryService;
        _repoService = repoService;
        WorkflowSettings = new WorkflowSettings();
    }

    public async Task<int> OnExecute(CommandLineApplication app, IConsole console)
    {
        try
        {
            PopulateDirectories();
            await GetAzureResources();

            if (AnsiConsole.Confirm($"Do you want to create the GitHub Secret for your Azure Publishing Profile (selecting no will leave value blank in workflow)?"))
            {
                var token = await _gitHubService.GetGitHubToken();
                var secrets = await _gitHubService.GetSecrets(WorkflowSettings.GitHubOwner, WorkflowSettings.GitHubRepo);
                bool writeSecret = true;
                foreach (var secret in secrets.Secrets)
                {
                    if (secret.Name == $"{WorkflowSettings.AzureResourceName.ToUpper()}_PUBLISH_PROFILE")
                    {
                        if (AnsiConsole.Confirm($"GitHub Secret {WorkflowSettings.AzureResourceName.ToUpper()}_PUBLISH_PROFILE exists, do you want to update it?"))
                        {
                            writeSecret = true;
                        }
                        else
                        {
                            writeSecret = false;
                        }
                        break;
                    }
                    else
                    {
                        writeSecret = true;
                    }
                }

                if (writeSecret)
                {
                    // UPDATE SECRET  
                    await _gitHubService.CreateSecret(WorkflowSettings.GitHubOwner, WorkflowSettings.GitHubRepo, $"{WorkflowSettings.AzureResourceName.ToUpper()}_PUBLISH_PROFILE", WorkflowSettings.AzurePublishProfile);
                }
            }

            PopulateWorkflow(console);
            return 0;
        }
        catch (CommandValidationException e)
        {
            _reporter.Error(e.Message);
            return 1;
        }
    }

    public async Task GetAzureResources()
    {
        var subscription = _azureService.GetSubscription();
        WorkflowSettings.AzureSubscription = subscription.Data.DisplayName;
        var resourceGroup = _azureService.GetResourceGroups();
        WorkflowSettings.AzureResourceGroup = resourceGroup.Data.Name;
        if (WorkflowSettings.AppType == AppType.Function)
        {
            var function = await _azureService.GetFunctions();
            WorkflowSettings.AzureResourceName = function.Name;
            if (function.Inner.Kind.Contains("linux"))
            {
                WorkflowSettings.AppPlatform = AppPlatform.Linux;
            }
            else
            {
                WorkflowSettings.AppPlatform = AppPlatform.Windows;
            }
        }
        else
        {
            var webapp = await _azureService.GetWebApps();
            WorkflowSettings.AzureResourceName = webapp.Name;
            if (webapp.Inner.Kind.Contains("linux"))
            {
                WorkflowSettings.AppPlatform = AppPlatform.Linux;
            }
            else
            {
                WorkflowSettings.AppPlatform = AppPlatform.Windows;
            }
        }
        WorkflowSettings.AzurePublishProfile = await _azureService.GetPublishProfile(WorkflowSettings.AzureResourceName);
    }

    public int PopulateWorkflow(IConsole console)
    {
        if (!Directory.Exists(WorkflowSettings.WorkflowFolderPath))
        {
            Directory.CreateDirectory(WorkflowSettings.WorkflowFolderPath);
        }

        string yaml;
        if (WorkflowSettings.AppType == AppType.Function)
        {
            yaml = AzureFunctionTemplate.Get("Build and Deploy",
                "main",
                WorkflowSettings.AzureResourceName,
                WorkflowSettings.PackagePath,
                WorkflowSettings.DOTNETVersion,
                WorkflowSettings.WorkingDirectory,
                WorkflowSettings.AppPlatform == AppPlatform.Linux ? "ubuntu" : "windows",
                "${{ secrets." + WorkflowSettings.AzureResourceName.ToUpper() + "_PUBLISH_PROFILE }}");
        }
        else
        {
            yaml = AzureWebAppTemplate.Get("Build and Deploy",
                "main",
                WorkflowSettings.AzureResourceName,
                WorkflowSettings.PackagePath,
                WorkflowSettings.DOTNETVersion,
                WorkflowSettings.WorkingDirectory,
                WorkflowSettings.AppPlatform == AppPlatform.Linux ? "ubuntu" : "windows",
                "${{ secrets." + WorkflowSettings.AzureResourceName.ToUpper() + "_PUBLISH_PROFILE }}");
        }

        if (string.IsNullOrEmpty(yaml) == false)
        {
            File.WriteAllText(System.IO.Path.Combine(WorkflowSettings.WorkflowFolderPath, "base.yml"), yaml);
        }

        AnsiConsole.Write($"GitHub Workflow Created at {WorkflowSettings.WorkflowFolderPath}");
        return 0;
    }

    private int PopulateDirectories()
    {
        // If no path is set, use the current directory
        if (string.IsNullOrEmpty(Path))
            Path = _fileSystem.Directory.GetCurrentDirectory();

        // Get all the projects

        AnsiConsole.WriteLine("Discovering project..");

        var projectPath = _projectDiscoveryService.DiscoverProject(Path);

        XmlSerializer ser = new XmlSerializer(typeof(WorkFlowGenerator.Models.Project));
        WorkFlowGenerator.Models.Project projectProperties = new WorkFlowGenerator.Models.Project();
        using (XmlReader reader = XmlReader.Create(projectPath))
        {
            projectProperties = (WorkFlowGenerator.Models.Project)ser.Deserialize(reader);
        }

        if (!string.IsNullOrEmpty(projectProperties.PropertyGroup.TargetFrameworks))
        {
            string[] frameworks = projectProperties.PropertyGroup.TargetFrameworks.Split(";");

            if (frameworks.Length > 1)
            {
                WorkflowSettings.DOTNETVersion = frameworks[frameworks.Length - 1].Replace("net", "") + ".x";
            }
            else if (frameworks.Length == 1)
            {
                WorkflowSettings.DOTNETVersion = frameworks[0].Replace("net", "") + ".x";
            }
            else
            {
                throw new Exception();
            }
        }
        else if (!string.IsNullOrEmpty(projectProperties.PropertyGroup.TargetFramework))
        {
            WorkflowSettings.DOTNETVersion = projectProperties.PropertyGroup.TargetFramework.Replace("net", "") + ".x";
        }
        else
        {
            throw new Exception();
        }

        if (!string.IsNullOrEmpty(projectProperties.PropertyGroup.AzureFunctionsVersion))
        {
            WorkflowSettings.AppType = AppType.Function;
        }
        else
        {
            WorkflowSettings.AppType = AppType.WebApp;
        }

        if (!string.IsNullOrEmpty(projectPath))
        {
            string workingDir = "";
            var repoInfo = _repoService.GetGitRepo(projectPath);
            string gitRepoRoot = repoInfo.Repository.Info.WorkingDirectory;
            //check if Git Repo
            if (!string.IsNullOrEmpty(gitRepoRoot))
            {
                workingDir = gitRepoRoot;
            }
            else
            {
                workingDir = System.IO.Path.GetDirectoryName(projectPath);
            }

            WorkflowSettings.GitHubRepo = repoInfo.GitHubRepo;
            WorkflowSettings.GitHubOwner = repoInfo.GitHubOwner;
            WorkflowSettings.WorkflowFolderPath = System.IO.Path.Combine(workingDir, ".github", "workflows");
            WorkflowSettings.WorkingDirectory = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(projectPath).Replace(workingDir, ""));
            WorkflowSettings.PackagePath = System.IO.Path.Combine(WorkflowSettings.WorkingDirectory, "publish");

            return 0;
        }
        else
        {
            _reporter.Error(string.Format(Resources.ValidationErrorMessages.DirectoryDoesNotContainSolutionsOrProjects, projectPath));
            return 1;
        }
    }

    public void ClearCurrentConsoleLine()
    {
        int currentLineCursor = Console.CursorTop;
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(new string(' ', Console.BufferWidth));
        Console.SetCursorPosition(0, currentLineCursor);
    }

}
