// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

internal sealed record class ValidatableEndpoint(
    ImmutableArray<ValidatableParameter> Parameters,
    ImmutableArray<ValidatableType> ValidatableTypes
);
