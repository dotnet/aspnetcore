// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.RequestDecompression;

/// <summary>
/// This is a placeholder for the <see cref="DecompressionProviderCollection"/> that allows
/// the creation of the given type via an <see cref="IServiceProvider"/>.
/// </summary>
internal class DecompressionProviderFactory : IDecompressionProvider
{
    public DecompressionProviderFactory(Type providerType)
    {
        ProviderType = providerType;
    }

    private Type ProviderType { get; }

    public IDecompressionProvider CreateInstance(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        return (IDecompressionProvider)ActivatorUtilities.CreateInstance(serviceProvider, ProviderType, Type.EmptyTypes);
    }

    string IDecompressionProvider.EncodingName
    {
        get { throw new NotSupportedException(); }
    }

    Stream IDecompressionProvider.CreateStream(Stream outputStream)
    {
        throw new NotSupportedException();
    }
}
