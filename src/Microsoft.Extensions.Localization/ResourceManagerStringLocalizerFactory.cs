// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.IO;
using System.Reflection;
using System.Resources;
using Microsoft.Dnx.Runtime;
using Microsoft.Extensions.OptionsModel;

namespace Microsoft.Extensions.Localization
{
    /// <summary>
    /// An <see cref="IStringLocalizerFactory"/> that creates instances of <see cref="ResourceManagerStringLocalizer"/>.
    /// </summary>
    public class ResourceManagerStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly IResourceNamesCache _resourceNamesCache = new ResourceNamesCache();

        private readonly IApplicationEnvironment _applicationEnvironment;

        private readonly string _resourcesRelativePath;

        /// <summary>
        /// Creates a new <see cref="ResourceManagerStringLocalizer"/>.
        /// </summary>
        /// <param name="applicationEnvironment">The <see cref="IApplicationEnvironment"/>.</param>
        /// <param name="localizationOptions">The <see cref="IOptions{LocalizationOptions}"/>.</param>
        public ResourceManagerStringLocalizerFactory(
            IApplicationEnvironment applicationEnvironment,
            IOptions<LocalizationOptions> localizationOptions)
        {
            if (applicationEnvironment == null)
            {
                throw new ArgumentNullException(nameof(applicationEnvironment));
            }

            if (localizationOptions == null)
            {
                throw new ArgumentNullException(nameof(localizationOptions));
            }

            _applicationEnvironment = applicationEnvironment;
            _resourcesRelativePath = localizationOptions.Value.ResourcesPath ?? string.Empty;
            if (!string.IsNullOrEmpty(_resourcesRelativePath))
            {
                _resourcesRelativePath = _resourcesRelativePath.Replace(Path.AltDirectorySeparatorChar, '.')
                    .Replace(Path.DirectorySeparatorChar, '.') + ".";
            }
        }

        /// <summary>
        /// Creates a <see cref="ResourceManagerStringLocalizer"/> using the <see cref="Assembly"/> and
        /// <see cref="Type.FullName"/> of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="resourceSource">The <see cref="Type"/>.</param>
        /// <returns>The <see cref="ResourceManagerStringLocalizer"/>.</returns>
        public IStringLocalizer Create(Type resourceSource)
        {
            if (resourceSource == null)
            {
                throw new ArgumentNullException(nameof(resourceSource));
            }

            var typeInfo = resourceSource.GetTypeInfo();
            var assembly = typeInfo.Assembly;
            var baseName = _applicationEnvironment.ApplicationName + "." + _resourcesRelativePath + resourceSource.Name;

            return new ResourceManagerStringLocalizer(
                new ResourceManager(baseName, assembly),
                assembly,
                baseName,
                _resourceNamesCache);
        }

        /// <summary>
        /// Creates a <see cref="ResourceManagerStringLocalizer"/>.
        /// </summary>
        /// <param name="baseName">The base name of the resource to load strings from.</param>
        /// <param name="location">The location to load resources from.</param>
        /// <returns>The <see cref="ResourceManagerStringLocalizer"/>.</returns>
        public IStringLocalizer Create(string baseName, string location)
        {
            if (baseName == null)
            {
                throw new ArgumentNullException(nameof(baseName));
            }

            var rootPath = location ?? _applicationEnvironment.ApplicationName;
            var assembly = Assembly.Load(new AssemblyName(rootPath));
            baseName = rootPath + "." + _resourcesRelativePath + baseName;

            return new ResourceManagerStringLocalizer(
                new ResourceManager(baseName, assembly),
                assembly,
                baseName,
                _resourceNamesCache);
        }
    }
}