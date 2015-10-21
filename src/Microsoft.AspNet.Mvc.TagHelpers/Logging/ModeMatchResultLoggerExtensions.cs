using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.TagHelpers.Internal;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.TagHelpers.Logging
{
    internal static class ModeMatchResultLoggerExtensions
    {
        private static readonly Action<ILogger, ITagHelper, string, Exception> _skippingProcessing;

        static ModeMatchResultLoggerExtensions()
        {
            _skippingProcessing = LoggerMessage.Define<ITagHelper, string>(
                LogLevel.Verbose,
                1,
                "Skipping processing for tag helper '{TagHelper}' with id '{TagHelperId}'.");
        }

        public static void TagHelperPartialMatches<TMode>(
                    this ILogger logger,
                    string uniqueId,
                    string viewPath,
                    IEnumerable<ModeMatchAttributes<TMode>> partialMatches)
        {
            var logValues = new PartialModeMatchLogValues<TMode>(
                uniqueId,
                viewPath,
                partialMatches);

            logger.LogWarning(logValues);
        }

        public static void TagHelperSkippingProcessing(
                    this ILogger logger,
                    ITagHelper tagHelper,
                    string uniqueId)
        {
            _skippingProcessing(
                logger,
                tagHelper,
                uniqueId,
                null);
        }

        /// <summary>
        /// Log values for <see cref="AspNet.Razor.TagHelpers.ITagHelper"/> instances that opt out
        /// of processing due to missing attributes for one of several possible modes.
        /// </summary>
        private class PartialModeMatchLogValues<TMode> : ILogValues
        {
            private readonly IEnumerable<ModeMatchAttributes<TMode>> _partialMatches;
            private readonly string _uniqueId;
            private readonly string _viewPath;

            /// <summary>
            /// Creates a new <see cref="PartialModeMatchLogValues{TMode}"/>.
            /// </summary>
            /// <param name="uniqueId">
            /// The unique ID of the HTML element this message applies to.
            /// </param>
            /// <param name="viewPath">The path to the view.</param>
            /// <param name="partialMatches">The set of modes with partial required attributes.</param>
            public PartialModeMatchLogValues(
                string uniqueId,
                string viewPath,
                IEnumerable<ModeMatchAttributes<TMode>> partialMatches)
            {
                if (partialMatches == null)
                {
                    throw new ArgumentNullException(nameof(partialMatches));
                }

                _uniqueId = uniqueId;
                _viewPath = viewPath;
                _partialMatches = partialMatches;
            }

            public IEnumerable<KeyValuePair<string, object>> GetValues()
            {
                yield return new KeyValuePair<string, object>(
                    "Message",
                    "Tag helper had partial matches while determining mode.");
                yield return new KeyValuePair<string, object>("UniqueId", _uniqueId);
                yield return new KeyValuePair<string, object>("ViewPath", _viewPath);
                yield return new KeyValuePair<string, object>("PartialMatches", _partialMatches);
            }

            public override string ToString()
            {
                var newLine = Environment.NewLine;
                return string.Format(
                    $"Tag Helper with ID '{_uniqueId}' in view '{_viewPath}' had partial matches " +
                    $"while determining mode:{newLine}\t{{0}}",
                        string.Join($"{newLine}\t", _partialMatches.Select(partial =>
                            string.Format($"Mode '{partial.Mode}' missing attributes:{newLine}\t\t{{0}} ",
                                string.Join($"{newLine}\t\t", partial.MissingAttributes)))));
            }
        }
    }
}
