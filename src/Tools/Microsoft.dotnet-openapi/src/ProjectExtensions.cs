// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Evaluation;

namespace Microsoft.DotNet.OpenApi;

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
