// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.ResponseCompression
{
    /// <summary>
    /// This is a placeholder for the CompressionProviderCollection that allows creating the given type via
    /// an <see cref="IServiceProvider" />.
    /// </summary>
    internal class CompressionProviderFactory : ICompressionProvider
    {
        public CompressionProviderFactory(Type providerType)
        {
            ProviderType = providerType;
        }

        private Type ProviderType { get; }

        public ICompressionProvider CreateInstance(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

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
}