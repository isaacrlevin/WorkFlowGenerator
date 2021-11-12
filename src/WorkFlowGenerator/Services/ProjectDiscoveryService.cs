using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using WorkFlowGenerator.Exceptions;

namespace WorkFlowGenerator.Services
{
    public class ProjectDiscoveryService : IProjectDiscoveryService
    {
        private readonly IFileSystem _fileSystem;

        public ProjectDiscoveryService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public string DiscoverProject(string path)
        {
            if (!(_fileSystem.File.Exists(path) || _fileSystem.Directory.Exists(path)))
                throw new CommandValidationException(string.Format(Resources.ValidationErrorMessages.DirectoryOrFileDoesNotExist, path));

            var fileAttributes = _fileSystem.File.GetAttributes(path);

            // If a directory was passed in, search for a .sln or .csproj file
            if (fileAttributes.HasFlag(FileAttributes.Directory))
            {
                // We did not find any solutions, so try and find individual projects
                var projectFiles = _fileSystem.Directory.GetFiles(path, "*.csproj").Concat(_fileSystem.Directory.GetFiles(path, "*.fsproj")).ToArray();
                if (projectFiles.Length == 1)
                    return  _fileSystem.Path.GetFullPath(projectFiles[0]) ;

                if (projectFiles.Length > 1)
                    throw new CommandValidationException(string.Format(Resources.ValidationErrorMessages.DirectoryContainsMultipleProjects, path));

                // At this point the path contains no solutions or projects, so throw an exception
                throw new CommandValidationException(string.Format(Resources.ValidationErrorMessages.DirectoryDoesNotContainSolutionsOrProjects, path));
            }

            if (
                (string.Compare(_fileSystem.Path.GetExtension(path), ".csproj", StringComparison.OrdinalIgnoreCase) == 0) ||
                (string.Compare(_fileSystem.Path.GetExtension(path), ".fsproj", StringComparison.OrdinalIgnoreCase) == 0))
                return  _fileSystem.Path.GetFullPath(path);

            // At this point, we know the file passed in is not a valid project or solution
            throw new CommandValidationException(string.Format(Resources.ValidationErrorMessages.FileNotAValidSolutionOrProject, path));
        }
    }
}