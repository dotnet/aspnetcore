// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

/// <summary>
/// Provides custom serialization logic for values of type <typeparamref name="T"/> stored in
/// <see cref="ProtectedLocalStorage"/> or <see cref="ProtectedSessionStorage"/>.
/// </summary>
/// <remarks>
/// <para>
/// Register an implementation in the dependency injection container to control how a type is
/// serialized. Values are otherwise serialized as JSON, which requires either reflection-based
/// serialization or a registered <c>JsonSerializerContext</c> describing the type.
/// </para>
/// <para>
/// Supplying a serializer is the way to store a type in an application published with Native AOT
/// or trimming enabled when the type is not covered by a source-generated serializer context.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of the value to serialize.</typeparam>
/// <example>
/// <code>
/// public sealed class ThemeSerializer : ProtectedBrowserStorageSerializer&lt;Theme&gt;
/// {
///     public override string Serialize(Theme value) => value.Name;
///
///     public override Theme Deserialize(string data) => new Theme(data);
/// }
///
/// builder.Services.AddSingleton&lt;ProtectedBrowserStorageSerializer&lt;Theme&gt;, ThemeSerializer&gt;();
/// </code>
/// </example>
[Experimental("ASPNETCORE9004", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public abstract class ProtectedBrowserStorageSerializer<T>
{
    /// <summary>
    /// Serializes the supplied <paramref name="value"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The serialized representation of <paramref name="value"/>.</returns>
    public abstract string Serialize(T value);

    /// <summary>
    /// Deserializes a value of type <typeparamref name="T"/> from the supplied <paramref name="data"/>.
    /// </summary>
    /// <param name="data">The serialized representation produced by <see cref="Serialize(T)"/>.</param>
    /// <returns>The deserialized value.</returns>
    public abstract T Deserialize(string data);
}
