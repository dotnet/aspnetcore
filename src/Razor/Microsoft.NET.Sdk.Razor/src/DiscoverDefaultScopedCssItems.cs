// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class DiscoverDefaultScopedCssItems : Task
    {
        [Required]
        public ITaskItem[] Content { get; set; }

        [Output]
        public ITaskItem[] DiscoveredScopedCssInputs { get; set; }

        public override bool Execute()
        {
            var discoveredInputs = new List<ITaskItem>();

            for (var i = 0; i < Content.Length; i++)
            {
                var candidate = Content[i];
                var fullPath = candidate.GetMetadata("FullPath");
                if (fullPath.EndsWith(".razor.css", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals("false", candidate.GetMetadata("Scoped"), StringComparison.OrdinalIgnoreCase))
                {
                    discoveredInputs.Add(candidate);
                }
            }

            DiscoveredScopedCssInputs = discoveredInputs.ToArray();

            return !Log.HasLoggedErrors;
        }
    }
}
