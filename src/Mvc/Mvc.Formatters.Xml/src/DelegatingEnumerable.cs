// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Linq;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

/// <summary>
/// Serializes <see cref="IEnumerable{T}"/> types by delegating them through a concrete implementation.
/// </summary>
/// <typeparam name="TWrapped">The wrapping or original type of the <see cref="IEnumerable{T}"/>
/// to proxy.</typeparam>
/// <typeparam name="TDeclared">The type parameter of the original <see cref="IEnumerable{T}"/>
/// to proxy.</typeparam>
public class DelegatingEnumerable<TWrapped, TDeclared> : IEnumerable<TWrapped>
{
    private readonly IEnumerable<TDeclared> _source;
    private readonly IWrapperProvider? _wrapperProvider;

    /// <summary>
    /// Initializes a <see cref="DelegatingEnumerable{TWrapped, TDeclared}"/>.
    /// </summary>
    /// <remarks>
    /// This constructor is necessary for <see cref="System.Runtime.Serialization.DataContractSerializer"/>
    /// to serialize.
    /// </remarks>
    public DelegatingEnumerable()
    {
        _source = Enumerable.Empty<TDeclared>();
    }

    /// <summary>
    /// Initializes a <see cref="DelegatingEnumerable{TWrapped, TDeclared}"/> with the original
    ///  <see cref="IEnumerable{T}"/> and the wrapper provider for wrapping individual elements.
    /// </summary>
    /// <param name="source">The <see cref="IEnumerable{T}"/> instance to get the enumerator from.</param>
    /// <param name="elementWrapperProvider">The wrapper provider for wrapping individual elements.</param>
    public DelegatingEnumerable(IEnumerable<TDeclared> source, IWrapperProvider elementWrapperProvider)
    {
        ArgumentNullException.ThrowIfNull(source);

        _source = source;
        _wrapperProvider = elementWrapperProvider;
    }

    /// <summary>
    /// Gets a delegating enumerator of the original <see cref="IEnumerable{T}"/> source which is being
    /// wrapped.
    /// </summary>
    /// <returns>The delegating enumerator of the original <see cref="IEnumerable{T}"/> source.</returns>
    public IEnumerator<TWrapped> GetEnumerator()
    {
        return new DelegatingEnumerator<TWrapped, TDeclared>(_source.GetEnumerator(), _wrapperProvider);
    }

    /// <summary>
    /// The serializer requires every type it encounters can be serialized and deserialized.
    /// This type will never be used for deserialization, but we are required to implement the add
    /// method so that the type can be serialized. This will never be called.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <exception cref="NotImplementedException">Thrown unconditionally.</exception>
    public void Add(object item)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets a delegating enumerator of the original <see cref="IEnumerable{T}"/> source which is being
    /// wrapped.
    /// </summary>
    /// <returns>The delegating enumerator of the original <see cref="IEnumerable{T}"/> source.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
