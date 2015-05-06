// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Reflection;
using System.Resources;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.Localization
{
    /// <summary>
    /// An <see cref="IStringLocalizerFactory"/> that creates instances of <see cref="ResourceManagerStringLocalizer"/>.
    /// </summary>
    public class ResourceManagerStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly IApplicationEnvironment _applicationEnvironment;

        /// <summary>
        /// Creates a new <see cref="ResourceManagerStringLocalizer"/>.
        /// </summary>
        /// <param name="applicationEnvironment"></param>
        public ResourceManagerStringLocalizerFactory(IApplicationEnvironment applicationEnvironment)
        {
            _applicationEnvironment = applicationEnvironment;
        }

        /// <summary>
        /// Creates a <see cref="ResourceManagerStringLocalizer"/> using the <see cref="Assembly"/> and
        /// <see cref="Type.FullName"/> of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="resourceSource">The <see cref="Type"/>.</param>
        /// <returns>The <see cref="ResourceManagerStringLocalizer"/>.</returns>
        public IStringLocalizer Create(Type resourceSource)
        {
            var typeInfo = resourceSource.GetTypeInfo();
            var assembly = typeInfo.Assembly;
            var baseName = typeInfo.FullName;
            return new ResourceManagerStringLocalizer(new ResourceManager(resourceSource), assembly, baseName);
        }

        /// <summary>
        /// Creates a <see cref="ResourceManagerStringLocalizer"/>.
        /// </summary>
        /// <param name="baseName">The base name of the resource to load strings from.</param>
        /// <param name="location">The location to load resources from.</param>
        /// <returns>The <see cref="ResourceManagerStringLocalizer"/>.</returns>
        public IStringLocalizer Create(string baseName, string location)
        {
            var assembly = Assembly.Load(new AssemblyName(location ?? _applicationEnvironment.ApplicationName));

            return new ResourceManagerStringLocalizer(new ResourceManager(baseName, assembly), assembly, baseName);
        }
    }
}