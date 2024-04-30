// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace JsonSchemaMapper;

/// <summary>
/// Controls the nullable behavior of reference types in the generated schema.
/// </summary>
#if EXPOSE_JSON_SCHEMA_MAPPER
public
#else
internal
#endif
    enum ReferenceTypeNullability
{
    /// <summary>
    /// Always treat reference types as nullable. Follows the built-in behavior
    /// of the serializer (cf. https://github.com/dotnet/runtime/issues/1256).
    /// </summary>
    AlwaysNullable,

    /// <summary>
    /// Treat reference types as nullable only if they are annotated with a nullable reference type modifier.
    /// </summary>
    Annotated,

    /// <summary>
    /// Always treat reference types as non-nullable.
    /// </summary>
    NeverNullable,
}
