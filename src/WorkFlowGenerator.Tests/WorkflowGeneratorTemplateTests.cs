using GitHubActionsDotNet.Helpers;
using GitHubActionsDotNet.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WorkFlowGenerator.Templates;

namespace WorkFlowGenerator.Tests;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TestClass]
public class WorkflowGeneratorTemplateTests
{
    [TestMethod]
    public void FunctionTest()
    {
        //Arrange
        string workflow_name = "Workflow generator for functions";
        string branch_name = "main";
        string azure_resource_name = "myazurefunction";
        string package_path = "function/function.zip";
        string dotnet_version = "3.1.x";
        string project_root = "src/";
        string platform = "windows";
        string publishProfileName = "${{ secrets.PUBLISH_PROFILE }}";

        //Act
        string yaml = AzureFunctionTemplate.Get(workflow_name,
            branch_name,
            azure_resource_name,
            package_path,
            dotnet_version,
            project_root,
            platform,
            publishProfileName);

        //Assert
        string expected = @"
name: Workflow generator for functions
on:
  push:
    branches:
    - main
env:
  AZURE_FUNCTIONAPP_NAME: myazurefunction
  AZURE_FUNCTIONAPP_PACKAGE_PATH: function/function.zip
  CONFIGURATION: Release
  DOTNET_CORE_VERSION: 3.1.x
  WORKING_DIRECTORY: src/
  DOTNET_CLI_TELEMETRY_OPTOUT: 1
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: 1
  DOTNET_NOLOGO: true
  DOTNET_GENERATE_ASPNET_CERTIFICATE: false
  DOTNET_ADD_GLOBAL_TOOLS_TO_PATH: false
  DOTNET_MULTILEVEL_LOOKUP: 0
jobs:
  build:
    name: Build job
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v2
    - name: Setup .NET Core
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: ${{ env.DOTNET_CORE_VERSION }}
    - name: Restore
      run: dotnet restore ${{ env.WORKING_DIRECTORY }}
    - name: Build
      run: dotnet build ${{ env.WORKING_DIRECTORY }} --configuration ${{ env.CONFIGURATION }} --no-restore
    - name: Test
      run: dotnet test
    - name: Publish
      run: dotnet publish ${{ env.WORKING_DIRECTORY }} --configuration ${{ env.CONFIGURATION }} --output ${{ env.AZURE_FUNCTIONAPP_PACKAGE_PATH }} --no-build
    - name: Deploy to Azure Function App
      uses: Azure/functions-action@v1
      with:
        app-name: ${{ env.AZURE_FUNCTIONAPP_NAME }}
        publish-profile: ${{ secrets.PUBLISH_PROFILE }}
        package: ${{ env.AZURE_FUNCTIONAPP_PACKAGE_PATH }}
";
        expected = Utility.TrimNewLines(expected);
        Assert.AreEqual(expected, yaml);
    }

    [TestMethod]
    public void WebAppTest()
    {
        //Arrange
        string workflow_name = "Workflow generator for webapps";
        string branch_name = "main";
        string azure_resource_name = "myazurewebapp";
        string package_path = "webapp/webapp.zip";
        string dotnet_version = "3.1.x";
        string project_root = "src/";
        string platform = "windows";
        string publishProfileName = "${{ secrets.PUBLISH_PROFILE }}";

        //Act
        string yaml = AzureWebAppTemplate.Get(workflow_name,
            branch_name,
            azure_resource_name,
            package_path,
            dotnet_version,
            project_root,
            platform,
            publishProfileName);

        //Assert
        string expected = @"
name: Workflow generator for webapps
on:
  push:
    branches:
    - main
env:
  AZURE_WEBAPP_NAME: myazurewebapp
  AZURE_WEBAPP_PACKAGE_PATH: webapp/webapp.zip
  CONFIGURATION: Release
  DOTNET_CORE_VERSION: 3.1.x
  WORKING_DIRECTORY: src/
  DOTNET_CLI_TELEMETRY_OPTOUT: 1
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: 1
  DOTNET_NOLOGO: true
  DOTNET_GENERATE_ASPNET_CERTIFICATE: false
  DOTNET_ADD_GLOBAL_TOOLS_TO_PATH: false
  DOTNET_MULTILEVEL_LOOKUP: 0
jobs:
  build:
    name: Build job
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v2
    - name: Setup .NET Core
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: ${{ env.DOTNET_CORE_VERSION }}
    - name: Restore
      run: dotnet restore ${{ env.WORKING_DIRECTORY }}
    - name: Build
      run: dotnet build ${{ env.WORKING_DIRECTORY }} --configuration ${{ env.CONFIGURATION }} --no-restore
    - name: Test
      run: dotnet test
    - name: Publish
      run: dotnet publish ${{ env.WORKING_DIRECTORY }} --configuration ${{ env.CONFIGURATION }} --output ${{ env.AZURE_WEBAPP_PACKAGE_PATH }} -r win-x86 --self-contained true
    - name: Deploy to Azure Web App
      uses: Azure/webapps-deploy@v2
      with:
        app-name: ${{ env.AZURE_WEBAPP_NAME }}
        publish-profile: ${{ secrets.PUBLISH_PROFILE }}
        package: ${{ env.AZURE_WEBAPP_PACKAGE_PATH }}
";
        expected = Utility.TrimNewLines(expected);
        Assert.AreEqual(expected, yaml);
    }
}
