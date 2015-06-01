// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Resources;
using Microsoft.Framework.Localization;
using Microsoft.Framework.Localization.Internal;

namespace LocalizationWebSite
{
    public class TestStringLocalizer : IStringLocalizer
    {
        private readonly IResourceNamesCache _resourceNamesCache;
        private ResourceManager _resourceManager;
        private readonly AssemblyWrapper _resourceAssemblyWrapper;
        private readonly string _resourceBaseName;
        private string _applicationBasePath;

        public TestStringLocalizer(ResourceManager resourceManager,
            AssemblyWrapper resourceAssembly,
            string baseName,
            IResourceNamesCache resourceNamesCache,
            string applicationBasePath)
        {
            _resourceAssemblyWrapper = resourceAssembly;
            _resourceManager = resourceManager;
            _resourceBaseName = baseName;
            _resourceNamesCache = resourceNamesCache;
            _applicationBasePath = applicationBasePath;
        }

        public virtual LocalizedString this[string name]
        {
            get
            {
                var value = GetStringSafely(name, null);
                return new LocalizedString(name, value ?? name, resourceNotFound: value == null);
            }
        }

        public virtual LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var format = GetStringSafely(name, null);
                var value = string.Format(format ?? name, arguments);
                return new LocalizedString(name, value, resourceNotFound: format == null);
            }
        }

        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            return new TestStringLocalizer(_resourceManager,
                    _resourceAssemblyWrapper,
                    _resourceBaseName,
                    _resourceNamesCache,
                    _applicationBasePath);
        }

        public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeAncestorCultures) =>
            GetAllStrings(includeAncestorCultures, CultureInfo.CurrentUICulture);

        protected IEnumerable<LocalizedString> GetAllStrings(bool includeAncestorCultures, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        protected string GetStringSafely(string name, CultureInfo culture)
        {
            var resourceValue = string.Empty;
#if DNX451
            var cultureName = (culture ?? CultureInfo.CurrentUICulture).Name;
            var resourceFile = _resourceManager.BaseName.Substring(_resourceManager.BaseName.IndexOf('.') + 1) + "." + cultureName;
            var filePath = Path.Combine(_applicationBasePath, "Resources", "bin");

            if (File.Exists(Path.Combine(filePath, resourceFile + ".resources")))
            {
                _resourceManager = ResourceManager.CreateFileBasedResourceManager(resourceFile, filePath, null);
            }
#endif
            try
            {
                // retrieve the value of the specified key
                resourceValue = _resourceManager.GetString(name);
            }
            catch (MissingManifestResourceException)
            {
                return name;
            }
            return resourceValue;
        }
    }
}
