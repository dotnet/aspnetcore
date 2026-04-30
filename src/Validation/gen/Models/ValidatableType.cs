// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.Extensions.Validation;

internal sealed record class ValidatableType(
    string TypeFQN,
    // TODO: This ImmutableArray is likely not equatable.
    // TODO: Use https://github.com/dotnet/runtime/blob/033df020f70a9ce98f97ac9a1a7ffa0e7306ee2c/src/libraries/Common/src/SourceGenerators/ImmutableEquatableArray.cs
    ImmutableArray<ValidatableProperty> Members
);
