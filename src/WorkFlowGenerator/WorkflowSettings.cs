namespace WorkFlowGenerator
{
    public class WorkflowSettings
    {
        public string WorkflowName { get; set; }
        public string AzureSubscription { get; set; }

        public string AzureResourceGroup { get; set; }

        public AppType AppType { get; set; }

        public AppPlatform AppPlatform { get; set; }

        public string AzureResourceName { get; set; }

        public string DOTNETVersion {get; set;}

        public string AzurePublishProfile { get; set; }

        public string PackagePath { get; set; }

        public string WorkflowFolderPath { get; set; }

        public string WorkingDirectory { get; set; }

        public string GitHubOwner { get; set; }

        public string GitHubRepo { get; set; }
    }

    public enum AppType
    {
        WebApp,
        Function
    }

    public enum AppPlatform
    {
        Windows,
        Linux
    }
}
