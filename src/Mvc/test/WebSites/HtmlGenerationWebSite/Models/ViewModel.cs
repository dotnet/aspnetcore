// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace HtmlGenerationWebSite.Models;

public class ViewModel
{
    public int Integer { get; set; } = 23;

    public long? NullableLong { get; set; } = 24L;

    public TemplateModel Template { get; set; } = new SuperTemplateModel();
}
