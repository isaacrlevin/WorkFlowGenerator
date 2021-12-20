using GitHubActionsDotNet.Helpers;
using GitHubActionsDotNet.Models;

namespace WorkFlowGenerator.Templates;

public static class AzureFunctionTemplate
{

    public static string Get(
        string workflow_name = "Workflow generator for functions",
        string branch_name = "main",
        string azure_resource_name = "myazurefunction",
        string package_path = "function/function.zip",
        string dotnet_version = "3.1.x",
        string project_root = "src/",
        string platform = "windows")
    {
        //Arrange
        GitHubActionsRoot root = new();
        root.name = workflow_name;
        root.on = TriggerHelper.AddStandardPushTrigger(branch_name);
        root.env = new()
        {
            { "AZURE_FUNCTIONAPP_NAME", azure_resource_name },
            { "AZURE_FUNCTIONAPP_PACKAGE_PATH", package_path },
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
            DotNetStepHelper.AddDotNetPublishStep("Publish","${{ env.WORKING_DIRECTORY }}", "${{ env.CONFIGURATION }}", "${{ env.AZURE_FUNCTIONAPP_PACKAGE_PATH }}", "--no-build"),
            AzureStepHelper.AddAzureFunctionDeployStep("Deploy to Azure Function App","${{ env.AZURE_FUNCTIONAPP_NAME }}", "${{ env.AZURE_FUNCTIONAPP_PACKAGE_PATH }}")
        };
        root.jobs = new();
        Job buildJob = JobHelper.AddJob(
            "Build job",
            platform + "-latest",
            buildSteps,
            null,
            null,
            0);
        root.jobs.Add("build", buildJob);

        //Act
        string yaml = GitHubActionsDotNet.Serialization.GitHubActionsSerialization.Serialize(root);
        return yaml;
    }


}
