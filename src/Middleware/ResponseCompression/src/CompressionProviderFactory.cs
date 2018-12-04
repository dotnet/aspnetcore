// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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