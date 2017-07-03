using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.AspNetCore.SpaServices.Prerendering;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up prerendering features in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class PrerenderingServiceCollectionExtensions
    {
        /// <summary>
        /// Configures the dependency injection system to supply an implementation
        /// of <see cref="ISpaPrerenderer"/>.
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/>.</param>
        public static void AddSpaPrerenderer(this IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            serviceCollection.AddSingleton<ISpaPrerenderer, DefaultSpaPrerenderer>();
        }
    }
}
