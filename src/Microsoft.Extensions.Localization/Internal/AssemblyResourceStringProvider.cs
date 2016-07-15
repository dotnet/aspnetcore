// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;

namespace Microsoft.Extensions.Localization.Internal
{
    public class AssemblyResourceStringProvider : IResourceStringProvider
    {
        private const string AssemblyElementDelimiter = ", ";
        private static readonly string[] _assemblyElementDelimiterArray = new[] { AssemblyElementDelimiter };
        private static readonly char[] _assemblyEqualDelimiter = new[] { '=' };

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
            var assemblyName = ApplyCultureToAssembly(culture);

            return $"Assembly={assemblyName};resourceName={_resourceBaseName}";
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
            var assemblyString = ApplyCultureToAssembly(culture);
            Assembly assembly;
            try
            {
                assembly = Assembly.Load(new AssemblyName(assemblyString));
            }
            catch (FileNotFoundException)
            {
                return null;
            }

            return new AssemblyWrapper(assembly);
        }

        // This is all a workaround for https://github.com/dotnet/coreclr/issues/6123
        private string ApplyCultureToAssembly(CultureInfo culture)
        {
            var builder = new StringBuilder(_assembly.FullName);

            var cultureName = string.IsNullOrEmpty(culture.Name) ? "neutral" : culture.Name;
            var cultureString = $"Culture={cultureName}";

            var cultureStartIndex = _assembly.FullName.IndexOf("Culture", StringComparison.OrdinalIgnoreCase);
            if (cultureStartIndex < 0)
            {
                builder.Append(AssemblyElementDelimiter + cultureString);
            }
            else
            {
                var cultureEndIndex = _assembly.FullName.IndexOf(
                    AssemblyElementDelimiter,
                    cultureStartIndex,
                    StringComparison.Ordinal);
                var cultureLength = cultureEndIndex - cultureStartIndex;
                builder.Remove(cultureStartIndex, cultureLength);
                builder.Insert(cultureStartIndex, cultureString);
            }

            var firstSplit = _assembly.FullName.IndexOf(AssemblyElementDelimiter);
            if (firstSplit < 0)
            {
                //Index of end of Assembly name
                firstSplit = _assembly.FullName.Length;
            }
            builder.Insert(firstSplit, ".resources");

            return builder.ToString();
        }
    }
}
