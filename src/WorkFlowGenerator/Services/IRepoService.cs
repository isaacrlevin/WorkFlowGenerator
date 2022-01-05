﻿using System;
using System.Collections.Generic;
using System.Text;
using LibGit2Sharp;
using WorkFlowGenerator.Models.GitHub;

namespace WorkFlowGenerator.Services;

public interface IRepoService
{
    public bool IsGitRepo(string path);

    public void CreateGitRepo(string path);
    public RepositoryExtended GetGitRepo(string path);

    public void CommitAndPushToRepo(string path, string workflowFilePath, string accessToken);
}
