// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Keyed services don't provide an accessible API for resolving
/// all the service keys associated with a given type.
/// See https:///github.com/dotnet/runtime/issues/100105 for more info.
/// This internal class is used to track the document names that have been registered
/// so that they can be resolved in the `IDocumentProvider` implementation.
/// This is inspired by the implementation used in Orleans. See
/// https:///github.com/dotnet/orleans/blob/005ab200bc91302245857cb75efaa436296a1aae/src/Orleans.Runtime/Hosting/NamedService.cs.
/// </summary>
internal sealed class NamedService<TService>(string name)
{
    public string Name { get; } = name;
}
