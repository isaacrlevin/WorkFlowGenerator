cd src/WorkFlowGenerator
dotnet pack
dotnet tool install -g --add-source /nupkg dotnet-workflow-generator
