﻿using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Identity.Extensions;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Spectre.Console;
using System.Collections.Generic;
using System.Net.Http;

namespace WorkFlowGenerator.Services;

public class AzureService : IAzureService
{
    private ArmClient _armClient;
    private AzureCliCredential _credential;
    private AzureIdentityFluentCredentialAdapter _credentialAdapter;
    private Subscription _subscription;
    private ResourceGroup _resourceGroup;
    private Azure.Core.AccessToken _accessToken;

    public AzureService()
    {
        _credential = new AzureCliCredential();
        _armClient = new ArmClient(_credential);
    }

    public Subscription GetSubscription()
    {
        List<Subscription> subscriptions = null;
        AnsiConsole.Status()
        .Start("Gathering Subscriptions...", ctx =>
        {
            subscriptions = _armClient.GetSubscriptions().ToList();
        });

        var pickedSub = AnsiConsole.Prompt(
            new SelectionPrompt<(string, string)>()
            .Title("What Azure subscription would you like to deploy to?")
            .PageSize(10)
            .AddChoices(subscriptions.Select(a => (a.Data.SubscriptionGuid, a.Data.DisplayName))));

        _subscription = subscriptions.Where(a => a.Data.SubscriptionGuid == pickedSub.Item1).FirstOrDefault();

        AnsiConsole.WriteLine($"The selected subscription is {_subscription.Data.SubscriptionGuid}");

        _accessToken = _credential.GetTokenAsync(new Azure.Core.TokenRequestContext(new[] { "https://management.azure.com/.default" })).Result;
        return _subscription;
    }

    public ResourceGroup GetResourceGroups()
    {
        List<ResourceGroup> resourceGroups = null;
        AnsiConsole.Status().Start("Gathering Resource Groups...", ctx =>
        {
            resourceGroups = _subscription.GetResourceGroups().ToList();
        });

        var pickedRg = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
            .Title($"What Azure Resource Group in {_subscription.Data.DisplayName} would you like to deploy to?")
            .PageSize(10)
            .MoreChoicesText("[grey](Move up and down to reveal more resource groups)[/]")
            .AddChoices(resourceGroups.Select(a => a.Data.Name)));

        _resourceGroup = resourceGroups.Where(a => a.Data.Name == pickedRg).FirstOrDefault();

        AnsiConsole.WriteLine($"The selected Resource Group is {_resourceGroup.Data.Name}");

        return _resourceGroup;
    }

    public async Task<IWebApp> GetWebApps()
    {

        if (_credentialAdapter == null)
        {
            _credentialAdapter = new AzureIdentityFluentCredentialAdapter(_credential, _subscription.Data.TenantId, AzureEnvironment.AzureGlobalCloud);
        }
        List<IWebApp> websites = null;
        AnsiConsole.Status().Start("Gathering Web Apps...", ctx =>
        {
            websites = Microsoft.Azure.Management.Fluent.Azure
                .Authenticate(_credentialAdapter)
                .WithSubscription(_subscription.Data.DisplayName)
                .AppServices.WebApps.ListByResourceGroupAsync(_resourceGroup.Data.Name).Result.ToList();
        });

        var pickedwebsites = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
            .Title($"What Azure Web App in {_resourceGroup.Data.Name} would you like to deploy to?")
            .PageSize(10)
            .MoreChoicesText("[grey](Move up and down to reveal more resource groups)[/]")
            .AddChoices(websites.Select(a => a.Name)));

        var webapp = websites.Where(a => a.Name == pickedwebsites).FirstOrDefault();

        AnsiConsole.WriteLine($"The selected Web App is {webapp.Name}");
        return webapp;
    }

    public async Task<IFunctionApp> GetFunctions()
    {
        if (_credentialAdapter == null)
        {
            _credentialAdapter = new AzureIdentityFluentCredentialAdapter(_credential, _subscription.Data.TenantId, AzureEnvironment.AzureGlobalCloud);
        }

        List<IFunctionApp> functions = null;
        AnsiConsole.Status().Start("Gathering Functions...", ctx =>
        {
            functions = Microsoft.Azure.Management.Fluent.Azure
              .Authenticate(_credentialAdapter)
              .WithSubscription(_subscription.Data.DisplayName)
              .AppServices.FunctionApps.ListByResourceGroupAsync(_resourceGroup.Data.Name).Result.ToList();
        });

        var pickedFunctions = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
            .Title($"What Azure Function in {_resourceGroup.Data.Name} would you like to deploy to?")
            .PageSize(10)
            .MoreChoicesText("[grey](Move up and down to reveal more resource groups)[/]")
            .AddChoices(functions.Select(a => a.Name)));

        var function = functions.Where(a => a.Name == pickedFunctions).FirstOrDefault();
        AnsiConsole.WriteLine($"The selected Function is {function.Name}");
        return function;
    }

    public async Task<string> GetPublishProfile(string resource)
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new System.Uri("https://management.azure.com")
        };

        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken.Token);

        using var response = httpClient.PostAsync($"subscriptions/{_subscription.Data.DisplayName}/resourceGroups/{_resourceGroup.Data.Name}/providers/Microsoft.Web/sites/{resource}/publishxml?api-version=2018-02-01", new StringContent("{}", System.Text.Encoding.UTF8, "application/json")).Result;

        var content = await response.Content.ReadAsStringAsync();

        return content;
    }

    public List<string> GetAzureTargets(string projectType)
    {
        if (projectType == "web")
        {
            return new List<string>
        {
             AppTarget.WebApp,
             AppTarget.AzureFunction
        };
        }
        else
        {
            return new List<string>
            {
                AppTarget.WebJob
            };
        }
    }
}
