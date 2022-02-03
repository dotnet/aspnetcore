// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// Attribute annotated on ActionResult constructor and helper method parameters to indicate
/// that the parameter is used to set the "statusCode" for the ActionResult.
/// <para>
/// Analyzers match this parameter by type name. This allows users to annotate custom results \ custom helpers
/// with a user-defined attribute without having to expose this type.
/// </para>
/// <para>
/// This attribute is intentionally marked Inherited=false since the analyzer does not walk the inheritance graph.
/// </para>
/// </summary>
/// <example>
/// Annotated constructor parameter:
/// <code>
/// public StatusCodeResult([ActionResultStatusCode] int statusCode)
/// {
///     StatusCode = statusCode;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class ActionResultStatusCodeAttribute : Attribute
{
}
