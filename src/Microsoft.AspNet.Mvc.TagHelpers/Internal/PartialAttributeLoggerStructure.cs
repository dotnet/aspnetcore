// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.TagHelpers.Internal
{
    /// <summary>
    /// An <see cref="ILoggerStructure"/> for log messages regarding <see cref="ITagHelper"/> instances that opt out of
    /// processing due to missing attributes for one of several possible modes.
    /// </summary>
    public class PartialModeMatchLoggerStructure<TMode> : ILoggerStructure
    {
        private readonly string _uniqueId;
        private readonly IEnumerable<ModeMatchAttributes<TMode>> _partialMatches;
        private readonly IEnumerable<KeyValuePair<string, object>> _values;

        /// <summary>
        /// Creates a new <see cref="PartialModeMatchLoggerStructure{TMode}"/>.
        /// </summary>
        /// <param name="uniqueId">The unique ID of the HTML element this message applies to.</param>
        /// <param name="partialMatches">The set of modes with partial required attributes.</param>
        public PartialModeMatchLoggerStructure(
            string uniqueId,
            [NotNull] IEnumerable<ModeMatchAttributes<TMode>> partialMatches)
        {
            _uniqueId = uniqueId;
            _partialMatches = partialMatches;
            _values = new Dictionary<string, object>
            {
                ["UniqueId"] = _uniqueId,
                ["PartialMatches"] = partialMatches
            };
        }

        /// <summary>
        /// The log message.
        /// </summary>
        public string Message
        {
            get
            {
                return "Tag Helper has missing required attributes.";
            }
        }

        /// <summary>
        /// Gets the values associated with this structured log message.
        /// </summary>
        /// <returns>The values.</returns>
        public IEnumerable<KeyValuePair<string, object>> GetValues()
        {
            return _values;
        }

        /// <summary>
        /// Generates a human readable string for this structured log message.
        /// </summary>
        /// <returns>The message.</returns>
        public string Format()
        {
            var newLine = Environment.NewLine;
            return
                string.Format($"Tag Helper {_uniqueId} had partial matches while determining mode:{newLine}\t{{0}}",
                    string.Join($"{newLine}\t", _partialMatches.Select(partial =>
                        string.Format($"Mode '{partial.Mode}' missing attributes:{newLine}\t\t{{0}} ",
                            string.Join($"{newLine}\t\t", partial.MissingAttributes)))));
        }
    }
}