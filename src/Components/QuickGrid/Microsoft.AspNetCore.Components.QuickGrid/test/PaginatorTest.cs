// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.QuickGrid.Tests;

public class PaginatorTest
{
    [Fact]
    public void StylesUseLogicalMargin()
    {
        using var stream = typeof(PaginatorTest).Assembly.GetManifestResourceStream("Paginator.razor.css");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);

        var css = reader.ReadToEnd();

        Assert.Contains("margin-inline-start: auto;", css);
    }
}
