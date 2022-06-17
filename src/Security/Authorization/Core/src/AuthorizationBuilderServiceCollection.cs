// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// Used to configure authorization
/// </summary>
public class AuthorizationBuilderServiceCollection : AuthorizationBuilder, IServiceCollection
{
    /// <inheritdoc/>
    public AuthorizationBuilderServiceCollection(IServiceCollection services) : base(services)
    {
    }

    /// <inheritdoc/>
    public ServiceDescriptor this[int index]
    {
        get => Services[index];
        set => Services[index] = value;
    }

    /// <inheritdoc/>
    public int Count => Services.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => Services.IsReadOnly;

    /// <inheritdoc/>
    public void Add(ServiceDescriptor item) => Services.Add(item);

    /// <inheritdoc/>
    public void Clear() => Services.Clear();

    /// <inheritdoc/>
    public bool Contains(ServiceDescriptor item) => Services.Contains(item);

    /// <inheritdoc/>
    public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => Services.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public IEnumerator<ServiceDescriptor> GetEnumerator() => Services.GetEnumerator();

    /// <inheritdoc/>
    public int IndexOf(ServiceDescriptor item) => Services.IndexOf(item);

    /// <inheritdoc/>
    public void Insert(int index, ServiceDescriptor item) => Services.Insert(index, item);

    /// <inheritdoc/>
    public bool Remove(ServiceDescriptor item) => Services.Remove(item);

    /// <inheritdoc/>
    public void RemoveAt(int index) => Services.RemoveAt(index);

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => Services.GetEnumerator();
}
