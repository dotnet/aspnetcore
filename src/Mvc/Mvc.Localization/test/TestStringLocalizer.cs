// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.Localization.Test
{
    public class TestStringLocalizer : IStringLocalizer
    {
        private CultureInfo _culture { get; set; }

        public TestStringLocalizer() : this(null)
        {
        }

        public TestStringLocalizer(CultureInfo culture)
        {
            _culture = culture;
        }

        public LocalizedString this[string name]
        {
            get
            {
                var value = "Hello ";

                if (_culture != null)
                {
                    value = "Bonjour ";
                }
                return new LocalizedString(name, value + name);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var value = "Hello ";

                if (_culture != null)
                {
                    value = "Bonjour ";
                }

                string argument = string.Empty;
                foreach (var arg in arguments)
                {
                    argument = argument + " " + arg;
                }
                return new LocalizedString(name, value + name + argument);
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            var allStrings = new List<LocalizedString>();
            allStrings.Add(new LocalizedString("Hello", "World"));

            if (includeParentCultures)
            {
                allStrings.Add(new LocalizedString("Foo", "Bar"));
            }

            return allStrings;
        }

        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            return new TestStringLocalizer(culture);
        }
    }
}
