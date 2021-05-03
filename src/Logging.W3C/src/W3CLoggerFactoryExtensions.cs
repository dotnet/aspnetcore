using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Logging.W3C
{
    /// <summary>
    /// Extension methods for the <see cref="W3CLogger"/>.
    /// </summary>
    public static class W3CLoggerFactoryExtensions
    {
        /// <summary>
        /// Adds W3C Server Logging.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> for adding logging.</param>
        /// <returns></returns>
        public static ILoggingBuilder AddW3CLogger(this ILoggingBuilder builder)
        {
            return AddW3CLogger(builder, options => { });
        }

        /// <summary>
        /// Adds W3C Server Logging.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> for adding logging.</param>
        /// <param name="configureOptions">A delegate to configure the <see cref="W3CLoggerOptions"/>.</param>
        /// <returns></returns>
        public static ILoggingBuilder AddW3CLogger(this ILoggingBuilder builder, Action<W3CLoggerOptions> configureOptions)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, W3CLoggerProvider>());
            builder.Services.Configure<LoggerFilterOptions>(options =>
            {
                var rule = new LoggerFilterRule(typeof(Microsoft.Extensions.Logging.W3C.W3CLoggerProvider).ToString(), "Microsoft.AspNetCore.W3CLogging", LogLevel.Information, (provider, category, logLevel) =>
                {
                    return (provider.Equals(typeof(Microsoft.Extensions.Logging.W3C.W3CLoggerProvider).ToString()) && category.Equals("Microsoft.AspNetCore.W3CLogging")) && logLevel >= LogLevel.Information;
                });
                options.Rules.Add(rule);
            }
            );
            builder.Services.Configure(configureOptions);
            return builder;
        }
    }
}
