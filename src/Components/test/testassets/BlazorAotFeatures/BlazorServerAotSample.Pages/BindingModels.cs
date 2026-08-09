// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorServerAotSample.Pages;

public sealed class BindingRoot
{
    public string PublicField = "";

    public string PublicProperty { get; set; } = "";

    public BindingRow[] Rows { get; set; } = [new(), new()];

    public BindingBook Book { get; } = new();

    public string[] Tags { get; set; } = ["", ""];

    public int[] SelectedCodes { get; set; } = [];
}

public sealed class BindingRow
{
    public string Name { get; set; } = "";
}

public sealed class BindingBook
{
    private readonly BindingRow[] _rows = [new(), new(), new()];

    public BindingRow this[int index]
    {
        get => _rows[index];
        set => _rows[index] = value;
    }
}
