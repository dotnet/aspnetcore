// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components.Web.Internal;

namespace Microsoft.AspNetCore.Components.Web;

internal sealed class ComponentJsonMetadataOptions
{
    public IList<IJsonTypeInfoResolver> Resolvers { get; } = [WebPersistentStateJsonContext.Default];
}
