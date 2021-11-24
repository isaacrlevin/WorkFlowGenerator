using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Text;
using WorkFlowGenerator.Models.GitHub;

namespace WorkFlowGenerator.Services
{
    public interface IRepoService
    {
        public RepositoryExtended GetGitRepo(string path);

        public void CommitAndPushToRepo(string path, string workflowFilePath, string accessToken);
    }
}
