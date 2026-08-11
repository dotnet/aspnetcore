// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Infrastructure;

/// <summary>
/// Describes a single <see cref="ParameterAttribute"/> or cascading parameter property of a component,
/// together with the delegates that read and write it.
/// </summary>
/// <remarks>
/// <para>
/// Instances are produced by the Blazor Native AOT metadata source generator and reached through
/// <see cref="ComponentDescriptor.Parameters"/>. The framework binds parameter values through
/// <see cref="SetValue"/> instead of reflecting over the component type.
/// </para>
/// <para>
/// This API is experimental, unsupported, and subject to change or removal in any release.
/// </para>
/// </remarks>
/// <example>
/// The generated form of a <c>[Parameter] public string? Title { get; set; }</c> property is an object
/// initializer, because every member is <c>required init</c>:
/// <code>
/// new ComponentParameterDescriptor
/// {
///     Name = "Title",
///     ParameterType = typeof(string),
///     Attribute = new ParameterAttribute(),
///     SetValue = static (target, value) =&gt; ((Counter)target).Title = (string?)value,
///     GetValue = static target =&gt; ((Counter)target).Title,
/// }
/// </code>
/// </example>
[Experimental("ASPNETCORE9004", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public sealed class ComponentParameterDescriptor
{
    /// <summary>
    /// Gets the name of the property, used to match an incoming parameter value.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the declared type of the property.
    /// </summary>
    public required Type ParameterType { get; init; }

    /// <summary>
    /// Gets the attribute that gives the property its role.
    /// </summary>
    /// <remarks>
    /// This is either a <see cref="ParameterAttribute"/> — whose
    /// <see cref="ParameterAttribute.CaptureUnmatchedValues"/> the framework honors — or a
    /// <see cref="CascadingParameterAttributeBase"/>. Carrying the attribute instance rather than a
    /// projection of it is what lets a new cascading parameter kind work without a change here. A
    /// property declared with both kinds is described by its cascading attribute, because that is what
    /// gives the property its role.
    /// </remarks>
    public required Attribute Attribute { get; init; }

    /// <summary>
    /// Gets the delegate that assigns a value to the property on a component instance.
    /// </summary>
    public required Action<object, object?> SetValue { get; init; }

    /// <summary>
    /// Gets the delegate that reads the property's value from a component instance.
    /// </summary>
    /// <remarks>
    /// Required rather than optional because persisted parameters are read back out in order to be
    /// persisted, and emitting a getter the framework may not use costs a single lambda.
    /// </remarks>
    public required Func<object, object?> GetValue { get; init; }

    /// <summary>
    /// Gets the delegate that resolves the custom
    /// <c>PersistentComponentStateSerializer&lt;T&gt;</c> registered for this property's type, if any.
    /// </summary>
    /// <remarks>
    /// Only meaningful for a property carrying a <see cref="PersistentStateAttribute"/>. The framework
    /// otherwise has to close an open generic over the property's runtime type, which is dynamic code;
    /// because the property's declared type is known at compile time, the generated form is an ordinary
    /// generic call. When this is <see langword="null"/> no custom serializer is used and the value
    /// round-trips as JSON.
    /// </remarks>
    /// <example>
    /// <code>
    /// GetStateSerializer = static services =&gt;
    ///     services.GetService&lt;PersistentComponentStateSerializer&lt;DashboardFilter&gt;&gt;(),
    /// </code>
    /// </example>
    public Func<IServiceProvider, object?>? GetStateSerializer { get; init; }
}
