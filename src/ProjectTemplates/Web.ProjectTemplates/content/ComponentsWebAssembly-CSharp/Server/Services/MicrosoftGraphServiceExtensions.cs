using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Identity.Web;

namespace ComponentsWebAssembly_CSharp.Server
{
    public static class MicrosoftGraphServiceExtensions
    {
        /// <summary>
        /// Adds the Microsoft Graph client as a singleton.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="initialScopes">Initial scopes.</param>
        /// <param name="graphBaseUrl">Base URL for Microsoft graph. This can be
        /// changed for instance for applications running in national clouds</param>
        public static IServiceCollection AddMicrosoftGraph(this IServiceCollection services,
                                             IEnumerable<string> initialScopes,
                                             string graphBaseUrl = "https://graph.microsoft.com/v1.0")
        {
            services.AddTokenAcquisition(true);
            services.AddSingleton<GraphServiceClient, GraphServiceClient>(serviceProvider =>
            {
                var tokenAquisitionService = serviceProvider.GetService<ITokenAcquisition>();
                GraphServiceClient client = string.IsNullOrWhiteSpace(graphBaseUrl) ?
                            new GraphServiceClient(new TokenAcquisitionCredentialProvider(tokenAquisitionService, initialScopes)) :
                            new GraphServiceClient(graphBaseUrl, new TokenAcquisitionCredentialProvider(tokenAquisitionService, initialScopes));
                return client;
            });
            return services;
        }
    }
}
