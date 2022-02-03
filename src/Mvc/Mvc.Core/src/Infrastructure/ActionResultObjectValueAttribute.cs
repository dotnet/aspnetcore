// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// Attribute annotated on ActionResult constructor, helper method parameters, and properties to indicate
/// that the parameter or property is used to set the "value" for ActionResult.
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
/// public BadRequestObjectResult([ActionResultObjectValue] object error)
///     :base(error)
/// {
///     StatusCode = DefaultStatusCode;
/// }
/// </code>
/// Annotated property:
/// <code>
/// public class ObjectResult : ActionResult, IStatusCodeActionResult
/// {
///     [ActionResultObjectValue]
///     public object Value { get; set; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class ActionResultObjectValueAttribute : Attribute
{
}
