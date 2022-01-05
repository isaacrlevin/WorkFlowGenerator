namespace WorkFlowGenerator;

public class WorkflowSettings
{
    public string WorkflowName { get; set; }

    public ProjectType ProjectType { get; set; }

    public string AzureSubscription { get; set; }

    public string AzureResourceGroup { get; set; }

    public string AppTarget { get; set; }

    public string AppPlatform { get; set; }

    public string AzureResourceName { get; set; }

    public string DOTNETVersion { get; set; }

    public string AzurePublishProfile { get; set; }

    public string PackagePath { get; set; }

    public string WorkflowFolderPath { get; set; }

    public string RepoRoot { get; set; }

    public string WorkingDirectory { get; set; }

    public string GitHubOwner { get; set; }

    public string GitHubRepo { get; set; }
}

public static class AppTarget
{
    public const string WebApp = "Azure Web App";
    public const string AzureFunction = "Azure Function";
    public const string WebJob = "Azure Web Job";
    public const string AzureKubernetesService = "Azure Kubernetes Service";
    public const string AzureContainerRegistry = "Azure Container Registry";
    public const string AzureContainerApps= "Azure Container Apps";
    public const string Nuget = "Nuget";
}

public enum ProjectType
{
    Console,
    WebApp,
    AzureFunction,
    ClassLibrary,
    Worker,
    BlazorWASM
}

public static class AppPlatform
{
    public const string Windows = "windows";
    public const string Linux = "ubuntu";
}
