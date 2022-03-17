// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;

/// <summary>
/// Serializes and deserializes the contents of <see cref="ITempDataDictionary"/>.
/// </summary>
public abstract class TempDataSerializer
{
    /// <summary>
    /// Deserializes <paramref name="unprotectedData"/> to a <see cref="IDictionary{TKey, TValue}"/>
    /// used to initialize an instance of <see cref="ITempDataDictionary"/>.
    /// </summary>
    /// <param name="unprotectedData">Serialized representation of <see cref="ITempDataDictionary"/>.</param>
    /// <returns>The deserialized <see cref="IDictionary{TKey, TValue}"/>.</returns>
    public abstract IDictionary<string, object> Deserialize(byte[] unprotectedData);

    /// <summary>
    /// Serializes the contents of <see cref="ITempDataDictionary"/>.
    /// </summary>
    /// <param name="values">The contents of <see cref="ITempDataDictionary"/>.</param>
    /// <returns>The serialized bytes.</returns>
    public abstract byte[] Serialize(IDictionary<string, object> values);

    /// <summary>
    /// Determines if the serializer supports the specified <paramref name="type"/>.
    /// <para>
    /// Defaults to returning <see langword="true"/> for all <see cref="Type"/> instances.
    /// </para>
    /// </summary>
    /// <param name="type">The <see cref="Type"/>.</param>
    /// <returns><see langword="true"/> if the serializer supports serializing <paramref name="type"/>, otherwise <see langword="false"/>.</returns>
    public virtual bool CanSerializeType(Type type) => true;
}
