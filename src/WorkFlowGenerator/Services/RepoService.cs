using System;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using WorkFlowGenerator.Models.GitHub;

namespace WorkFlowGenerator.Services;

public class RepoService : IRepoService
{
    public bool IsGitRepo(string path)
    {
        if (!string.IsNullOrEmpty(Repository.Discover(path)))
        {
            return true;
        }
        return false;
    }

    public void CreateGitRepo(string path)
    {
        Repository.Init(path);
    }

    public RepositoryExtended GetGitRepo(string path)
    {
        var repo = new Repository(Repository.Discover(path));

        RepositoryExtended repoExtended = new RepositoryExtended
        {
            Repository = repo,
        };

        if (repo.Config.Get<string>("remote.origin.url") != null)
        {
            var url = new Uri(repo.Config.Get<string>("remote.origin.url").Value);
            repoExtended.RemoteOriginUrl = url;
            repoExtended.GitHubOwner = url.Segments[url.Segments.Length - 2].Replace("/", "");
            repoExtended.GitHubRepo = url.Segments[url.Segments.Length - 1].Replace("/", "");
        }
        return repoExtended;
    }

    public void CommitAndPushToRepo(string path, string workflowFilePath, string accessToken)
    {
        using (var repo = new Repository(Repository.Discover(path)))
        {
            // Stage the file
            repo.Index.Add(workflowFilePath);
            repo.Index.Write();

            Configuration config = repo.Config;
            Signature author = config.BuildSignature(DateTimeOffset.Now);

            // Commit to the repository
            Commit commit = repo.Commit("Adding GitHub Workflow file", author, author);

            //              LibGit2Sharp.PushOptions options = new LibGit2Sharp.PushOptions();

            //              options.CredentialsProvider = new CredentialsHandler(
            //(url, usernameFromUrl, types) => new UsernamePasswordCredentials()
            //{
            //    Username = "isaacrlevin",
            //    Password = accessToken
            //});

            //              repo.Network.Push(repo.Branches[repo.Head.FriendlyName], options);


        }
    }
}
