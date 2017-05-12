// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;

namespace Microsoft.Extensions.Localization.Internal
{
    public class AssemblyResourceStringProvider : IResourceStringProvider
    {
        private readonly AssemblyWrapper _assembly;
        private readonly string _resourceBaseName;
        private readonly IResourceNamesCache _resourceNamesCache;

        public AssemblyResourceStringProvider(
            IResourceNamesCache resourceCache,
            AssemblyWrapper resourceAssembly,
            string resourceBaseName)
        {
            _resourceNamesCache = resourceCache;
            _assembly = resourceAssembly;
            _resourceBaseName = resourceBaseName;
        }

        private string GetResourceCacheKey(CultureInfo culture)
        {
            var assemblyName = new AssemblyName(_assembly.FullName)
            {
                CultureName = culture.Name
            };

            return $"Assembly={assemblyName.FullName};resourceName={_resourceBaseName}";
        }

        private string GetResourceName(CultureInfo culture)
        {
            var resourceStreamName = _resourceBaseName;
            if (!string.IsNullOrEmpty(culture.Name))
            {
                resourceStreamName += "." + culture.Name;
            }
            resourceStreamName += ".resources";

            return resourceStreamName;
        }

        private IList<string> ThrowOrNull(CultureInfo culture, bool throwOnMissing)
        {
            if (throwOnMissing)
            {
                throw new MissingManifestResourceException(
                    Resources.FormatLocalization_MissingManifest(GetResourceName(culture)));
            }

            return null;
        }

        public IList<string> GetAllResourceStrings(CultureInfo culture, bool throwOnMissing)
        {
            var cacheKey = GetResourceCacheKey(culture);

            return _resourceNamesCache.GetOrAdd(cacheKey, _ =>
            {
                var assembly = GetAssembly(culture);
                if (assembly == null)
                {
                    return ThrowOrNull(culture, throwOnMissing);
                }

                var resourceStreamName = GetResourceName(culture);
                using (var resourceStream = assembly.GetManifestResourceStream(resourceStreamName))
                {
                    if (resourceStream == null)
                    {
                        return ThrowOrNull(culture, throwOnMissing);
                    }

                    using (var resources = new ResourceReader(resourceStream))
                    {
                        var names = new List<string>();
                        foreach (DictionaryEntry entry in resources)
                        {
                            var resourceName = (string)entry.Key;
                            names.Add(resourceName);
                        }
                        return names;
                    }
                }
            });
        }

        protected virtual AssemblyWrapper GetAssembly(CultureInfo culture)
        {
            Assembly assembly;
            var assemblyName = new AssemblyName(_assembly.FullName)
            {
                CultureName = culture.Name
            };
            try
            {
                assembly = Assembly.Load(assemblyName);
            }
            catch (FileNotFoundException)
            {
                return null;
            }

            return new AssemblyWrapper(assembly);
        }
    }
}
