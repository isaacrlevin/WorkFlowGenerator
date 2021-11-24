using Azure.ResourceManager.Resources;
using Microsoft.Azure.Management.AppService.Fluent;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WorkFlowGenerator.Services
{
    public interface IAzureService
    {
        public Subscription GetSubscription();

        public ResourceGroup GetResourceGroups();

        public Task<IWebApp> GetWebApps();

        public Task<IFunctionApp> GetFunctions();
    }
}
