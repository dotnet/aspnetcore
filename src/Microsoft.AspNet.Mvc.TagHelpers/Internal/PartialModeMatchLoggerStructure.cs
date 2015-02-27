// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.TagHelpers.Internal
{
    /// <summary>
    /// An <see cref="ILoggerStructure"/> for log messages regarding <see cref="ITagHelper"/> instances that opt out of
    /// processing due to missing attributes for one of several possible modes.
    /// </summary>
    public abstract class PartialModeMatchLoggerStructure : ILoggerStructure
    {
        private readonly IEnumerable<KeyValuePair<string, object>> _values;

        protected PartialModeMatchLoggerStructure(IEnumerable<KeyValuePair<string, object>> values)
        {
            _values = values;
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
        /// Returns a human-readable string of the structured data.
        /// </summary>
        public abstract string Format();

        /// <summary>
        /// Gets the values associated with this structured log message.
        /// </summary>
        /// <returns>The values.</returns>
        public IEnumerable<KeyValuePair<string, object>> GetValues()
        {
            return _values;
        }
    }
}