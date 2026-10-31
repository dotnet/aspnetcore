// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.ResponseCompression;

/// <summary>
/// This is a placeholder for the CompressionProviderCollection that allows creating the given type via
/// an <see cref="IServiceProvider" />.
/// </summary>
internal sealed class CompressionProviderFactory : ICompressionProvider
{
    public CompressionProviderFactory([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type providerType)
    {
        ProviderType = providerType;
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    private Type ProviderType { get; }

    public ICompressionProvider CreateInstance(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return (ICompressionProvider)ActivatorUtilities.CreateInstance(serviceProvider, ProviderType, Type.EmptyTypes);
    }

    string ICompressionProvider.EncodingName
    {
        get { throw new NotSupportedException(); }
    }

    bool ICompressionProvider.SupportsFlush
    {
        get { throw new NotSupportedException(); }
    }

    Stream ICompressionProvider.CreateStream(Stream outputStream)
    {
        throw new NotSupportedException();
    }
}
