// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

public interface ITempData
{
    object? this[string key] { get; set; }
    object? Get(string key);
    object? Peek(string key);
    void Keep();
    void Keep(string key);

    //TO-DO: Add Save to save and clean-up after request
}
