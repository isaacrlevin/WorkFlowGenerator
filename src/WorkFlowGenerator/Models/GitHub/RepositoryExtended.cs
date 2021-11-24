using System;
using System.Collections.Generic;
using System.Text;
using LibGit2Sharp;

namespace WorkFlowGenerator.Models.GitHub;

public class RepositoryExtended
{
    public Uri RemoteOriginUrl { get; set; }

    public string GitHubOwner { get; set; }

    public string GitHubRepo { get; set; }

    public Repository Repository { get; set; }
}
