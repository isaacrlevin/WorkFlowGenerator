using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WorkFlowGenerator.Exceptions;
using WorkFlowGenerator.Services;

namespace WorkFlowGenerator
{
    internal class Program
    {
        private readonly IFileSystem _fileSystem;
        private readonly IReporter _reporter;
        private readonly IProjectDiscoveryService _projectDiscoveryService;

        [Argument(0, Description = "The path to a .csproj or .fsproj file, or to a directory containing a .NET Core solution/project. " +
                           "If none is specified, the current directory will be used.")]
        public string Path { get; set; }


        public static int Main(string[] args)
        {
            using (
                var services = new ServiceCollection()
                    .AddSingleton<IConsole>(PhysicalConsole.Singleton)
                    .AddSingleton<IReporter>(provider => new ConsoleReporter(provider.GetService<IConsole>()))
                    .AddSingleton<IFileSystem, FileSystem>()
                    .AddSingleton<IProjectDiscoveryService, ProjectDiscoveryService>()
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

        public Program(IFileSystem fileSystem, IReporter reporter, IProjectDiscoveryService projectDiscoveryService)
        {
            _fileSystem = fileSystem;
            _reporter = reporter;
            _projectDiscoveryService = projectDiscoveryService;
        }

        public async Task<int> OnExecute(CommandLineApplication app, IConsole console)
        {
            try
            {
                // If no path is set, use the current directory
                if (string.IsNullOrEmpty(Path))
                    Path = _fileSystem.Directory.GetCurrentDirectory();

                // Get all the projects
                console.Write("Discovering project...");

                var projectPath = _projectDiscoveryService.DiscoverProject(Path);

                if (!string.IsNullOrEmpty(projectPath))
                {
                    var workingDir = System.IO.Path.GetDirectoryName(projectPath);
                    var workflowDir = System.IO.Path.Combine(workingDir, ".github", "workflows");
                    if (!Directory.Exists(workflowDir))
                    {
                        Directory.CreateDirectory(workflowDir);
                    }


                    var assembly = Assembly.GetEntryAssembly();
                    var foo = assembly.GetManifestResourceNames();
                    var resourceStream = assembly.GetManifestResourceStream("WorkFlowGenerator.templates.base.txt");
                    if (resourceStream != null)
                    {
                        using (var reader = new StreamReader(resourceStream, Encoding.UTF8))
                        {
                            var fileContents = await reader.ReadToEndAsync().ConfigureAwait(false);
                            var hydratedTemplate = HydrateTemplate(fileContents);
                            File.WriteAllText(System.IO.Path.Combine(workflowDir, "base.yml"), hydratedTemplate);
                        }
                    }

                    console.WriteLine($"GitHub Workflow Created at {workflowDir}");

                    return 0;
                }
                else
                {
                    _reporter.Error(string.Format(Resources.ValidationErrorMessages.DirectoryDoesNotContainSolutionsOrProjects, projectPath));

                    return 1;
                }

            }
            catch (CommandValidationException e)
            {
                _reporter.Error(e.Message);

                return 1;
            }
        }
        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.BufferWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        public static string HydrateTemplate(string template)
        {
            template = template.Replace("{NAME}", "Build and Deploy");
            template = template.Replace("{BRANCH_NAME}", "main");

            return template;
        }
    }
}
