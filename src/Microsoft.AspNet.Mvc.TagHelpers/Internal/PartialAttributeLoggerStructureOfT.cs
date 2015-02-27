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
    public class PartialModeMatchLoggerStructure<TMode> : PartialModeMatchLoggerStructure
    {
        private readonly string _uniqueId;
        private readonly string _viewPath;
        private readonly IEnumerable<ModeMatchAttributes<TMode>> _partialMatches;

        /// <summary>
        /// Creates a new <see cref="PartialModeMatchLoggerStructure{TMode}"/>.
        /// </summary>
        /// <param name="uniqueId">The unique ID of the HTML element this message applies to.</param>
        /// <param name="viewPath">The path to the view.</param>
        /// <param name="partialMatches">The set of modes with partial required attributes.</param>
        public PartialModeMatchLoggerStructure(
            string uniqueId,
            string viewPath,
            [NotNull] IEnumerable<ModeMatchAttributes<TMode>> partialMatches)
            : base(values: new Dictionary<string, object>
            {
                ["UniqueId"] = uniqueId,
                ["ViewPath"] = viewPath,
                ["PartialMatches"] = partialMatches
            })
        {
            _uniqueId = uniqueId;
            _viewPath = viewPath;
            _partialMatches = partialMatches;
        }
        
        /// <summary>
        /// Generates a human readable string for this structured log message.
        /// </summary>
        /// <returns>The message.</returns>
        public override string Format()
        {
            var newLine = Environment.NewLine;
            return string.Format(
                $"Tag Helper with ID {_uniqueId} in view '{_viewPath}' had partial matches while determining mode:{newLine}\t{{0}}",
                    string.Join($"{newLine}\t", _partialMatches.Select(partial =>
                        string.Format($"Mode '{partial.Mode}' missing attributes:{newLine}\t\t{{0}} ",
                            string.Join($"{newLine}\t\t", partial.MissingAttributes)))));
        }
    }
}