// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Represents a unique identifier that is used to store and retrieve
/// JSON schemas associated with a given property.
/// </summary>
internal record struct OpenApiSchemaKey(Type Type, ParameterInfo? ParameterInfo);
