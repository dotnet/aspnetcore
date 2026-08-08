// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Microsoft.JSInterop.Infrastructure;

/// <summary>
/// Describes one <see cref="JSInvokableAttribute"/> method and performs the whole invocation:
/// deserializing the arguments, calling the method, and serializing the result.
/// </summary>
/// <remarks>
/// <para>
/// Instances are produced by the Blazor Native AOT metadata source generator and reached through
/// <see cref="JSRuntime.InvokableMethods"/>. The descriptor owns the entire call rather than just the
/// method invocation, because the declared parameter and return types are exactly what the dispatcher
/// cannot name without reflection and exactly what the generator does know. Inside the generated body
/// the serializer calls are made on concrete type arguments, so they are statically analyzable and the
/// reflective surface disappears rather than moving.
/// </para>
/// <para>
/// This API is experimental, unsupported, and subject to change or removal in any release.
/// </para>
/// </remarks>
/// <example>
/// The generated form of <c>[JSInvokable] public void OnResize(ViewportSize size)</c> declared on a
/// <c>Widget</c>:
/// <code>
/// new JSInvokableMethodDescriptor
/// {
///     AssemblyName = "MyApp",
///     TargetType = typeof(Widget),
///     Identifier = "OnResize",
///     IsStatic = false,
///     Invoke = static (target, argsJson, options) =&gt; InvokeOnResize(target, argsJson, options),
/// }
/// </code>
/// </example>
[Experimental("ASPNETCORE9004", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
internal sealed class JSInvokableMethodDescriptor
{
    /// <summary>
    /// Gets the name of the assembly declaring the method, which is the key the wire protocol uses for
    /// a static invocation.
    /// </summary>
    public required string AssemblyName { get; init; }

    /// <summary>
    /// Gets the type declaring the method, which is the key the wire protocol uses for an instance
    /// invocation.
    /// </summary>
    /// <remarks>
    /// The framework walks the receiver's base-type chain against this value, so a
    /// <see cref="DotNetObjectReference{TValue}"/> over a derived type still finds a method declared on
    /// a base type, matching the behavior of the reflection-based lookup.
    /// </remarks>
    public required Type TargetType { get; init; }

    /// <summary>
    /// Gets the identifier JavaScript uses to name the method, which is either the value given to
    /// <see cref="JSInvokableAttribute"/> or the method's name.
    /// </summary>
    public required string Identifier { get; init; }

    /// <summary>
    /// Gets a value indicating whether the described method is static.
    /// </summary>
    /// <remarks>
    /// Static and instance methods use different wire-protocol lookup keys. A static method is keyed
    /// by <see cref="AssemblyName"/>, while an instance method is keyed by <see cref="TargetType"/>.
    /// </remarks>
    public required bool IsStatic { get; init; }

    /// <summary>
    /// Gets a stable key that identifies the generated method contribution.
    /// </summary>
    /// <remarks>
    /// Generated metadata contexts use this value to identify the same application-wide contribution
    /// when multiple contexts are registered. A <see langword="null"/> value is always treated as a
    /// distinct contribution.
    /// </remarks>
    public string? MethodKey { get; init; }

    /// <summary>
    /// Gets the inheritance behavior of the described method.
    /// </summary>
    public JSInvokableMethodKind Kind { get; init; }

    /// <summary>
    /// Gets the delegate that performs the invocation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The first argument is the receiver, which is <see langword="null"/> for a static method. The
    /// second is the JSON-encoded argument array as it arrived over the interop boundary, and the
    /// result is the JSON-encoded return value, or <see langword="null"/> when the method returns no
    /// value. Strings are used because that is what the boundary already deals in.
    /// </para>
    /// <para>
    /// The <see cref="JsonSerializerOptions"/> are passed in rather than captured, because descriptors
    /// are shared for the lifetime of the process while the options belong to a single runtime
    /// instance and hold converters closed over it.
    /// </para>
    /// </remarks>
    public required Func<object?, string, JsonSerializerOptions, ValueTask<string?>> Invoke { get; init; }
}

/// <summary>
/// Describes how a generated JS-invokable method participates in instance method inheritance.
/// </summary>
[Experimental("ASPNETCORE9004", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
internal enum JSInvokableMethodKind
{
    /// <summary>
    /// The method does not override a base method.
    /// </summary>
    Method,

    /// <summary>
    /// The method is virtual and can only be inherited by receiver types covered by generated metadata.
    /// </summary>
    Override,

    /// <summary>
    /// An unannotated override hides an annotated base method.
    /// </summary>
    OverrideBlocker,
}
