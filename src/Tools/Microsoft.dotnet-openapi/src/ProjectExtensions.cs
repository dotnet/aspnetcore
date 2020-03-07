// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Evaluation;

namespace Microsoft.DotNet.OpenApi
{
    public static class ProjectExtensions
    {
        public static void AddElementWithAttributes(this Project project, string tagName, string include, IDictionary<string, string> metadata)
        {
            var item = project.AddItem(tagName, include).Single();
            foreach (var kvp in metadata)
            {
                item.Xml.AddMetadata(kvp.Key, kvp.Value, expressAsAttribute: true);
            }

            project.Save();
        }
    }
}
