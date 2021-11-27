# WorkFlowGenerator

A .NET global tool to generate workflows for GitHub Actions based on project configuration and user inputs. The tool is published to Nuget as well

https://www.nuget.org/packages/dotnet-workflow-generator/

## Getting Started

The fastest way to use this tool is to install it using the .NET CLI

```cmd
dotnet tool install --global dotnet-workflow-generator
```

and if you want to update

```cmd
dotnet tool update --global dotnet-workflow-generator
```

After you have the tool installed, you can run it from a inside a directory with a `.csproj` in it, or pass the path to one directly

```cmd
REM Run tool from inside folder
cd C:\dev\somefolderwithaprojectfile
dotnet-workflow-generator

REM Pass path into tool
dotnet-workflow-generator C:\dev\somefolderwithaprojectfile
```

When you run the tool, it will parse the `.csproj` in the folder and prompt you for a target location to publish. You will select an Azure Subscription, a Resource Group, and an Azure Resource. If the app is an Azure Function (determined by the presence of the `AzureFunctionsVersion` property) it will only list Function Apps to deploy to, otherwise, it will only list Web Apps.

After you select your target, it will create a sample workflow based on the `TargetFramework` specified in the project file.

![Demo](static/demo.gif)

## Building from source

You can run this code from source after cloning the repo. Once you have the code locally, you can install the tool by running the included `.cmd/.sh` file

### Windows
```
git clone https://github.com/isaacrlevin/WorkFlowGenerator.git
cd WorkflowGenerator
createtool.cmd
```

### Mac/Linux
```
git clone https://github.com/isaacrlevin/WorkFlowGenerator.git
cd WorkflowGenerator
createtool.sh
```

At this point you will have whatever installed the latest version of the tool in the `main` branch. If you want to update the tool in the future, you can run the pack and update commands

```
dotnet pack
dotnet tool update -g --add-source .\nupkg dotnet-workflow-generator
```

You can also run the app from Visual Studio/VS Code/.NET CLI with the following command from the folder with WorkflowGenerator.csproj inside it

**NOTE: You will need to set a path of the .csproj you want to geneate an workflow for using Debug Profiles in VS or passing the path into the command line**

```
dotnet run C:\dev\somefolderwithaprojectfileinit
```
## Contributing

I would love folks to contribute to this idea. Currently I see 2 main forms of contribution that can take place and below are the steps folks can take

* You found a bug or some other issue (gap in docs, missing tests)
  *  Create an issue (than a PR?) detailing what your expectation is vs what you are getting
* You have an idea for additional functionality (want to add a new project type or target)
  * Create a discussion with your proposal and why you think it is a good idea.
  * More than likely I will ask you to do the work, so be ready 😊


## Open Source Tools Used in this project

* [Spectre.Console](https://github.com/spectreconsole/spectre.console)
* [Azure SDK](https://github.com/Azure/azure-sdk)
* [IdentityModel.OidcClient](https://github.com/IdentityModel/IdentityModel.OidcClient)
* [McMaster.Extensions.CommandLineUtils](https://github.com/natemcmaster/CommandLineUtils)
