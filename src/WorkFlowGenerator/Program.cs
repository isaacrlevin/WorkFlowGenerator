using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using System;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using WorkFlowGenerator.Exceptions;
using WorkFlowGenerator.Models;
using WorkFlowGenerator.Services;

namespace WorkFlowGenerator
{
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
                await PopulateWorkflow(console);
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

                //var publishingprofile = webapp.GetPublishingProfile();
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

                //var publishingprofile = webapp.GetPublishingProfile();
            }
        }

        public async Task<int> PopulateWorkflow(IConsole console)
        {
            if (!Directory.Exists(WorkflowSettings.WorkflowFolderPath))
            {
                Directory.CreateDirectory(WorkflowSettings.WorkflowFolderPath);
            }

            var assembly = Assembly.GetEntryAssembly();
            Stream resourceStream = null;
            if (WorkflowSettings.AppType == AppType.Function)
            {
                resourceStream = assembly.GetManifestResourceStream("WorkFlowGenerator.templates.function.txt");
            }
            else
            {
                resourceStream = assembly.GetManifestResourceStream("WorkFlowGenerator.templates.webapp.txt");
            }

            if (resourceStream != null)
            {
                using (var reader = new StreamReader(resourceStream, Encoding.UTF8))
                {
                    var fileContents = await reader.ReadToEndAsync().ConfigureAwait(false);
                    var hydratedTemplate = HydrateTemplate(fileContents);
                    File.WriteAllText(System.IO.Path.Combine(WorkflowSettings.WorkflowFolderPath, "base.yml"), hydratedTemplate);
                }
            }

            console.WriteLine($"GitHub Workflow Created at {WorkflowSettings.WorkflowFolderPath}");
            return 0;

        }

        private int PopulateDirectories()
        {
            // If no path is set, use the current directory
            if (string.IsNullOrEmpty(Path))
                Path = _fileSystem.Directory.GetCurrentDirectory();

            // Get all the projects
            Console.Write("Discovering project...");

            var projectPath = _projectDiscoveryService.DiscoverProject(Path);

            XmlSerializer ser = new XmlSerializer(typeof(WorkFlowGenerator.Models.Project));
            WorkFlowGenerator.Models.Project projectProperties = new WorkFlowGenerator.Models.Project();
            using (XmlReader reader = XmlReader.Create(projectPath))
            {
                projectProperties = (WorkFlowGenerator.Models.Project)ser.Deserialize(reader);
            }

            if (!string.IsNullOrEmpty(projectProperties.PropertyGroup.TargetFramework))
            {
                string[] frameworks = projectProperties.PropertyGroup.TargetFramework.Split(";");

                if (frameworks.Length > 1)
                {
                    WorkflowSettings.DOTNETVersion = frameworks[frameworks.Length-1].Replace("net", "") + ".x";
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

        public string HydrateTemplate(string template)
        {
            template = template.Replace("{WORKFLOW_NAME}", "Build and Deploy");
            template = template.Replace("{BRANCH_NAME}", "main");
            template = template.Replace("{PACKAGE_PATH}", WorkflowSettings.PackagePath);
            template = template.Replace("{AZURE_RESOURCE_NAME}", WorkflowSettings.AzureResourceName);
            template = template.Replace("{DOTNET_VERSION}", WorkflowSettings.DOTNETVersion);
            template = template.Replace("{PLATFORM}", WorkflowSettings.AppPlatform == AppPlatform.Linux ? "ubuntu" : "windows");
            template = template.Replace("{PROJECT_ROOT}", WorkflowSettings.WorkingDirectory);

            return template;
        }
    }
}
