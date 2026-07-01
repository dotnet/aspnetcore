// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation.Localization;

public readonly struct ValidationMessageKeyContext
{
    public ValidationAttribute Attribute { get; init; }

    public string DisplayName { get; init; }

    public Type? Type { get; init; }
}
