using System;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MsgPackProtocolDependencyInjectionExtensions
    {
        /// <summary>
        /// Enables the MsgPack protocol for SignalR.
        /// </summary>
        /// <remarks>
        /// This has no effect if the MsgPack protocol has already been enabled.
        /// </remarks>
        /// <param name="builder">The <see cref="ISignalRBuilder"/> representing the SignalR server to add MsgPack protocol support to.</param>
        /// <returns>The value of <paramref name="builder"/></returns>
        public static TBuilder AddMessagePackProtocol<TBuilder>(this TBuilder builder) where TBuilder : ISignalRBuilder
            => AddMessagePackProtocol(builder, _ => { });

        /// <summary>
        /// Enables the MsgPack protocol for SignalR and allows options for the MsgPack protocol to be configured.
        /// </summary>
        /// <remarks>
        /// Any options configured here will be applied, even if the MsgPack protocol has already been registered with the server.
        /// </remarks>
        /// <param name="builder">The <see cref="ISignalRBuilder"/> representing the SignalR server to add MsgPack protocol support to.</param>
        /// <param name="configure">A delegate that can be used to configure the <see cref="MessagePackHubProtocolOptions"/></param>
        /// <returns>The value of <paramref name="builder"/></returns>
        public static TBuilder AddMessagePackProtocol<TBuilder>(this TBuilder builder, Action<MessagePackHubProtocolOptions> configure) where TBuilder : ISignalRBuilder
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, MessagePackHubProtocol>());
            builder.Services.Configure(configure);
            return builder;
        }
    }
}
