// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

public class TestTagHelperFeature : RazorEngineFeatureBase, ITagHelperFeature
{
    public TestTagHelperFeature()
    {
        TagHelpers = new List<TagHelperDescriptor>();
    }

    public TestTagHelperFeature(IEnumerable<TagHelperDescriptor> tagHelpers)
    {
        TagHelpers = new List<TagHelperDescriptor>(tagHelpers);
    }

    public List<TagHelperDescriptor> TagHelpers { get; }

    public IReadOnlyList<TagHelperDescriptor> GetDescriptors()
    {
        return TagHelpers.ToArray();
    }
}
