// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Resources;
using Microsoft.Framework.Localization;
using Microsoft.Framework.Localization.Internal;
using Microsoft.Framework.Runtime;

namespace LocalizationWebSite
{
    public class TestStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly IApplicationEnvironment _applicationEnvironment;
        private readonly IResourceNamesCache _resourceNamesCache = new ResourceNamesCache();

        public TestStringLocalizerFactory(IApplicationEnvironment applicationEnvironment)
        {
            _applicationEnvironment = applicationEnvironment;
        }

        public IStringLocalizer Create(Type resourceSource)
        {
            var typeInfo = resourceSource.GetTypeInfo();
            var assembly = typeInfo.Assembly;
            var baseName = typeInfo.FullName;
            return new TestStringLocalizer(
                new ResourceManager(resourceSource),
                new AssemblyWrapper(assembly),
                baseName,
                _resourceNamesCache,
                _applicationEnvironment.ApplicationBasePath);
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            if (string.IsNullOrEmpty(location))
            {
                location = _applicationEnvironment.ApplicationName;
            }
            var assembly = Assembly.Load(new AssemblyName(location));

            return new TestStringLocalizer(
                new ResourceManager(baseName, assembly),
                new AssemblyWrapper(assembly),
                baseName,
                _resourceNamesCache,
                _applicationEnvironment.ApplicationBasePath);
        }
    }
}
