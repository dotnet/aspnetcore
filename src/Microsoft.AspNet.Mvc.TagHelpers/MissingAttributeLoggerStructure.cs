// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    /// <summary>
    /// An <see cref="ILoggerStructure"/> for log messages regarding <see cref="ITagHelper"/> instances that opt out of
    /// processing due to missing required attributes.
    /// </summary>
    public class MissingAttributeLoggerStructure : ILoggerStructure
    {
        private readonly string _uniqueId;
        private readonly IEnumerable<string> _missingAttributes;
        private readonly IEnumerable<KeyValuePair<string, object>> _values;

        /// <summary>
        /// Creates a new <see cref="MissingAttributeLoggerStructure"/>.
        /// </summary>
        /// <param name="uniqueId">The unique ID of the HTML element this message applies to.</param>
        /// <param name="missingAttributes">The missing required attributes.</param>
        public MissingAttributeLoggerStructure(string uniqueId, IEnumerable<string> missingAttributes)
        {
            _uniqueId = uniqueId;
            _missingAttributes = missingAttributes;
            _values = new Dictionary<string, object>
            {
                { "UniqueId", _uniqueId },
                { "MissingAttributes", _missingAttributes }
            };
        }

        /// <summary>
        /// The log message.
        /// </summary>
        public string Message
        {
            get
            {
                return "Tag Helper skipped due to missing required attributes.";
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
            return string.Format("Tag Helper unique ID: {0}, Missing attributes: {1}",
                _uniqueId,
                string.Join(",", _missingAttributes));
        }
    }
}