// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Logging;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.TagHelpers;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Utility related extensions for <see cref="TagHelperContext"/>.
    /// </summary>
    public static class TagHelperContextExtensions
    {
        /// <summary>
        /// Determines whether a <see cref="ITagHelper" />'s required attributes are present, non null, non empty, and
        /// non whitepsace.
        /// </summary>
        /// <param name="context">The <see cref="TagHelperContext"/>.</param>
        /// <param name="requiredAttributes">The attributes the <see cref="ITagHelper" /> requires in order to run.</param>
        /// <param name="logger">An optional <see cref="ILogger"/> to log warning details to.</param>
        /// <returns>A <see cref="bool"/> indicating whether the <see cref="ITagHelper" /> should run.</returns> 
        public static bool AllRequiredAttributesArePresent(
            [NotNull]this TagHelperContext context,
            [NotNull]IEnumerable<string> requiredAttributes,
            ILogger logger = null)
        {
            // Check for all attribute values & log a warning if any required are missing
            var atLeastOnePresent = false;
            var missingAttrNames = new List<string>();

            foreach (var attr in requiredAttributes)
            {
                if (!context.AllAttributes.ContainsKey(attr)
                    || context.AllAttributes[attr] == null
                    || string.IsNullOrWhiteSpace(context.AllAttributes[attr] as string))
                {
                    // Missing attribute!
                    missingAttrNames.Add(attr);
                }
                else
                {
                    atLeastOnePresent = true;
                }
            }

            if (missingAttrNames.Any())
            {
                if (atLeastOnePresent && logger != null && logger.IsEnabled(LogLevel.Warning))
                {
                    // At least 1 attribute was present indicating the user intended to use the tag helper,
                    // but at least 1 was missing too, so log a warning with the details.
                    logger.WriteWarning(new MissingAttributeLoggerStructure(context.UniqueId, missingAttrNames));
                }

                return false;
            }

            // All required attributes present
            return true;
        }
    }
}