using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.W3C
{
    /// <summary>
    /// A provider of <see cref="W3CLogger"/> instances.
    /// </summary>
    public class W3CLoggerProvider : ILoggerProvider
    {

        private readonly IOptionsMonitor<W3CLoggerOptions> _options;
        private readonly ConcurrentDictionary<string, W3CLogger> _loggers;

        /// <summary>
        /// Creates a new instance of <see cref="W3CLoggerProvider"/>
        /// </summary>
        /// <param name="options">The options to use when creating a provider.</param>
        public W3CLoggerProvider(IOptionsMonitor<W3CLoggerOptions> options)
        {
            _options = options;
            _loggers = new ConcurrentDictionary<string, W3CLogger>();
        }

        /// <summary>
        /// Creates a <see cref="W3CLogger"/> with the given <paramref name="categoryName"/>.
        /// </summary>
        /// <param name="categoryName">The name of the category to create this logger with.</param>
        /// <returns>The <see cref="W3CLogger"/> that was created.</returns>
        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.TryGetValue(categoryName, out var logger) ?
                logger :
                _loggers.GetOrAdd(categoryName, new W3CLogger(categoryName, _options));
        }

        /// <summary>
        /// Gets the full path of the log file corresponding with the given <paramref name="categoryName"/>,
        /// if there is a <see cref="W3CLogger"/> associated with that category.
        /// </summary>
        /// <param name="categoryName">The name of the category to look up.</param>
        /// <returns>
        /// The full path of the corresponding log file, or
        /// null if no <see cref="W3CLogger"/> associated with the given category.
        /// </returns>
        public string? GetLogFileFullName(string categoryName)
        {
            return _loggers.TryGetValue(categoryName, out var logger) ?
                logger.LogFileFullName :
                null;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (var val in _loggers.Values)
            {
                val.Dispose();
            }
        }
    }
}
