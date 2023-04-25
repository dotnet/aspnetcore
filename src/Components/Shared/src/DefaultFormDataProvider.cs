// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Binding;

internal sealed class DefaultFormDataProvider : FormDataProvider
{
    private static readonly IReadOnlyDictionary<string, StringValues> Empty =
        ReadOnlyDictionary<string, StringValues>.Empty;

    public override string? Name => null;

    public bool IsAvailable => Name != null;

    public override IReadOnlyDictionary<string, StringValues> Entries => Empty;
}
