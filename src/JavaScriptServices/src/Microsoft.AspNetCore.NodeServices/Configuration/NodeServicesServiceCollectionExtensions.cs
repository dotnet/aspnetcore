using System;
using Microsoft.AspNetCore.NodeServices;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up NodeServices in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class NodeServicesServiceCollectionExtensions
    {
        /// <summary>
        /// Adds NodeServices support to the <paramref name="serviceCollection"/>.
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/>.</param>
        public static void AddNodeServices(this IServiceCollection serviceCollection)
            => AddNodeServices(serviceCollection, _ => {});

        /// <summary>
        /// Adds NodeServices support to the <paramref name="serviceCollection"/>.
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="setupAction">A callback that will be invoked to populate the <see cref="NodeServicesOptions"/>.</param>
        public static void AddNodeServices(this IServiceCollection serviceCollection, Action<NodeServicesOptions> setupAction)
        {
            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof (setupAction));
            }

            serviceCollection.AddSingleton(typeof(INodeServices), serviceProvider =>
            {
                // First we let NodeServicesOptions take its defaults from the IServiceProvider,
                // then we let the developer override those options
                var options = new NodeServicesOptions(serviceProvider);
                setupAction(options);

                return NodeServicesFactory.CreateNodeServices(options);
            });
        }
    }
}
