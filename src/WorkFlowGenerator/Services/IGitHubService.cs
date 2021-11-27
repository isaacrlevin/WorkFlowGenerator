using System.Threading.Tasks;
using WorkFlowGenerator.Models.GitHub;

namespace WorkFlowGenerator.Services;

public interface IGitHubService
{
    public Task<string> GetGitHubToken();

    public Task<SecretRoot> GetSecrets(string owner, string repo);

    public Task CreateSecret(string owner, string repo, string secretName, string secretValue);

    public Task CreateAndCommit(string owner, string repo, string fileName);
}
