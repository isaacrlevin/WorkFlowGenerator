using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Octokit;
using WorkFlowGenerator.Models;
using WorkFlowGenerator.Models.GitHub;

namespace WorkFlowGenerator.Services;

public class GitHubService : IGitHubService
{
    GitHubClient _gitHub = new GitHubClient(new ProductHeaderValue("WorkflowGenerator"));
    HttpClient client = new HttpClient();
    private AppSettings _options;
    private string _accessToken;

    public GitHubService(Microsoft.Extensions.Options.IOptionsMonitor<AppSettings> optionsAccessor)
    {
        _options = optionsAccessor.CurrentValue;
        client.BaseAddress = new Uri("https://api.github.com");
        client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("WorkflowGenerator", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    }
    void OpenBrowser(string url)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (OperatingSystem.IsWindows())
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", url);
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", url);
            }
            else
            {
                throw;
            }
        }
    }
    public async Task<string> GetGitHubToken()
    {

        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        app.Run(async ctx =>
        {
            Task WriteResponse(HttpContext ctx)
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "text/html";
                return ctx.Response.WriteAsync("<h1>You can now return to the application.</h1>", Encoding.UTF8);
            }

            switch (ctx.Request.Method)
            {
                case "GET":
                    await WriteResponse(ctx);

                    tcs.TrySetResult(ctx.Request.QueryString.Value);
                    break;

                case "POST" when !ctx.Request.HasFormContentType:
                    ctx.Response.StatusCode = 415;
                    break;

                case "POST":
                    {
                        using var sr = new StreamReader(ctx.Request.Body, Encoding.UTF8);
                        var body = await sr.ReadToEndAsync();

                        await WriteResponse(ctx);

                        tcs.TrySetResult(body);
                        break;
                    }

                default:
                    ctx.Response.StatusCode = 405;
                    break;
            }
        });

        var browserPort = 7777;

        app.Urls.Add($"https://127.0.0.1:{browserPort}");

        app.Start();

        var timeout = TimeSpan.FromMinutes(5);

        string redirectUri = string.Format($"https://127.0.0.1:{browserPort}");
        string authUrl = $"https://github.com/login/oauth/authorize?client_id={_options.GitHubClientId}&scope=user%20repo&redirect_uri={HttpUtility.UrlEncode(redirectUri)}";

        OpenBrowser(authUrl);

        var code = await tcs.Task.WaitAsync(timeout);
        code = code.Replace("?code=", "");
        await app.DisposeAsync();

        string tokenRequestBody = string.Format("code={0}&client_id={1}&client_secret={2}",
code,
_options.GitHubClientId,
_options.GitHubClientSecret
);

        var formContent = new FormUrlEncodedContent(new[]
{
    new KeyValuePair<string, string>("code", code),
    new KeyValuePair<string, string>("client_id", _options.GitHubClientId),
    new KeyValuePair<string, string>("client_secret", _options.GitHubClientSecret)
});

        // sends the request
        HttpClient _client = new HttpClient();
        _client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        var response = await _client.PostAsync("https://github.com/login/oauth/access_token", formContent);

        string responseText = await response.Content.ReadAsStringAsync();

        Dictionary<string, string> tokenEndpointDecoded = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText);

        _accessToken = tokenEndpointDecoded["access_token"];

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Token", _accessToken);
        _gitHub.Credentials = new Credentials(_accessToken);
        return _accessToken;
    }

    public async Task CreateAndCommit(string owner, string repo, string fileName)
    {
        var headMasterRef = "heads/main";

        // Get reference of master branch
        var masterReference = await _gitHub.Git.Reference.Get(owner, repo, headMasterRef);
        // Get the laster commit of this branch
        var latestCommit = await _gitHub.Git.Commit.Get(owner, repo, masterReference.Object.Sha);

        // Create new Tree
        var nt = new NewTree { BaseTree = latestCommit.Tree.Sha };
        // Add items based on blobs
        nt.Tree.Add(new NewTreeItem { Path = fileName, Mode = "100644" });

        var newTree = await _gitHub.Git.Tree.Create(owner, repo, nt);

        // Create Commit
        var newCommit = new NewCommit("Adding GitHub Workflow file", newTree.Sha, masterReference.Object.Sha);
        var commit = await _gitHub.Git.Commit.Create(owner, repo, newCommit);
        await _gitHub.Git.Reference.Update(owner, repo, "heads/master", new ReferenceUpdate(commit.Sha, true));

    }

    public async Task<SecretRoot> GetSecrets(string owner, string repo)
    {
        string endpoint = $"/repos/{owner}/{repo}/actions/secrets";
        return await GetAsync<SecretRoot>(endpoint);
    }

    public async Task CreateSecret(string owner, string repo, string secretName, string secretValue)
    {
        var key = await GetAsync<Models.GitHub.PublicKey>($"/repos/{owner}/{repo}/actions/secrets/public-key");
        var secretValueBytes = System.Text.Encoding.UTF8.GetBytes(secretValue);
        var publicKey = Convert.FromBase64String(key.Key);
        var sealedPublicKeyBox = Sodium.SealedPublicKeyBox.Create(secretValueBytes, publicKey);

        var response = await PutAsync($"/repos/{owner}/{repo}/actions/secrets/{secretName}", sealedPublicKeyBox);
    }

    private async Task<TValue> GetAsync<TValue>(string endpoint)
    {
        return await client.GetFromJsonAsync<TValue>(endpoint);
    }

    private async Task<HttpResponseMessage> PutAsync(string endpoint, byte[] content)
    {
        return await client.PutAsync(endpoint, new ByteArrayContent(content));
    }
}
