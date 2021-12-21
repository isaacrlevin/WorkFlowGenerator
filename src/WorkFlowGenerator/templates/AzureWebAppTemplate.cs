using GitHubActionsDotNet.Helpers;
using GitHubActionsDotNet.Models;

namespace WorkFlowGenerator.Templates;

public static class AzureWebAppTemplate
{
    public static string Get(
        string workflow_name,
        string branch_name,
        string azure_resource_name,
        string package_path,
        string dotnet_version,
        string project_root,
        string platform,
        string publishProfileName)
    {
        //Arrange
        GitHubActionsRoot root = new();
        root.name = workflow_name;
        root.on = TriggerHelper.AddStandardPushTrigger(branch_name);
        root.env = new()
        {
            { "AZURE_WEBAPP_NAME", azure_resource_name },
            { "AZURE_WEBAPP_PACKAGE_PATH", package_path },
            { "CONFIGURATION", "Release" },
            { "DOTNET_CORE_VERSION", dotnet_version },
            { "WORKING_DIRECTORY", project_root },
            { "DOTNET_CLI_TELEMETRY_OPTOUT", "1" },
            { "DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1" },
            { "DOTNET_NOLOGO", "true" },
            { "DOTNET_GENERATE_ASPNET_CERTIFICATE", "false" },
            { "DOTNET_ADD_GLOBAL_TOOLS_TO_PATH", "false" },
            { "DOTNET_MULTILEVEL_LOOKUP", "0" }
        };
        Step[] buildSteps = new Step[] {
            CommonStepHelper.AddCheckoutStep(),
            DotNetStepHelper.AddDotNetSetupStep("Setup .NET Core","${{ env.DOTNET_CORE_VERSION }}"),
            DotNetStepHelper.AddDotNetRestoreStep("Restore","${{ env.WORKING_DIRECTORY }}"),
            DotNetStepHelper.AddDotNetBuildStep("Build","${{ env.WORKING_DIRECTORY }}","${{ env.CONFIGURATION }}","--no-restore"),
            DotNetStepHelper.AddDotNetTestStep("Test"),
            DotNetStepHelper.AddDotNetPublishStep("Publish","${{ env.WORKING_DIRECTORY }}", "${{ env.CONFIGURATION }}", "${{ env.AZURE_WEBAPP_PACKAGE_PATH }}", "-r win-x86 --self-contained true"),
            AzureStepHelper.AddAzureWebAppDeployStep("Deploy to Azure Web App","${{ env.AZURE_WEBAPP_NAME }}", "${{ env.AZURE_WEBAPP_PACKAGE_PATH }}", publishProfileName)
        };
        root.jobs = new();
        JobHelper jobHelper = new();
        Job buildJob = jobHelper.AddJob(
            "Build job",
            platform + "-latest",
            buildSteps);
        root.jobs.Add("build", buildJob);

        //Act
        return GitHubActionsDotNet.Serialization.GitHubActionsSerialization.Serialize(root);
    }
}
