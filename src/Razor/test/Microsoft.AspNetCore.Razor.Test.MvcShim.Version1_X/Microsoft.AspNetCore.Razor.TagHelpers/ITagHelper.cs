// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Razor.TagHelpers;

public interface ITagHelper
{
    int Order { get; }

    void Init(TagHelperContext context);

    Task ProcessAsync(TagHelperContext context, TagHelperOutput output);
}
