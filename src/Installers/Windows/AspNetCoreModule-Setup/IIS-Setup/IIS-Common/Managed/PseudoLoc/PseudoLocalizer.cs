// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Microsoft.Web.Utility
{
#if PSEUDOLOCALIZER_ENABLED || DEBUG

    /// <summary>
    /// Class that pseudo-localizes resources from a RESX file by intercepting calls to the managed wrapper class.
    /// </summary>
    internal static class PseudoLocalizer
    {
        private static bool _shouldPseudoLocalize;
        private static double _plocPaddingLengthRatio;

        static PseudoLocalizer()
        {
            _shouldPseudoLocalize = false;
            int plocLengthPaddingPercentage = 50;
#if DEBUG
            string plocValue = Environment.GetEnvironmentVariable("PSEUDOLOCALIZE");
            if (!string.IsNullOrEmpty(plocValue))
            {
                _shouldPseudoLocalize = true;
                int.TryParse(plocValue, out plocLengthPaddingPercentage);
                if (plocLengthPaddingPercentage < 10)
                {
                    plocLengthPaddingPercentage = 50;
                }
            }
#endif // DEBUG

            _plocPaddingLengthRatio = plocLengthPaddingPercentage * 0.01;
            if (_plocPaddingLengthRatio < 0.1)
            {
                DebugTrace("ploc should be at least 10% padded");
            }

#if PSEUDOLOCALIZER_ENABLED
            _shouldPseudoLocalize = true;
#endif // PSEUDOLOCALIZER_ENABLED
        }

        public static bool ShouldPseudoLocalize
        {
            get
            {
                return _shouldPseudoLocalize;
            }
        }

        // Need to use this method instead of tracing/debugging directly otherwise
        // the application will not be able to disable asserts from the exe.config file.
        // this is because this code is run very early on in the app before the
        // default trace listener has had a chance to initialize its settings
        // correctly
        private static void DebugTrace(string format, params object[] args)
        {
            ////only uncomment these lines when actually debugging something
            ////otherwise the application wont be able to toggle the assert ui
            ////if (args == null || args.Length == 0)
            ////{
            ////    Debug.WriteLine(format);
            ////}
            ////else
            ////{
            ////    Debug.WriteLine(format, args);
            ////}
        }

        /// <summary>
        /// Enables pseudo-localization on any type that is of the form {AssemblyName}.Resources
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns>true if succeeded, false if failed.</returns>
        public static bool TryEnableAssembly(Assembly assembly)
        {
            bool retVal = false;
            if (assembly != null)
            {
                AssemblyName assemblyName = assembly.GetName();
                string resourceTypeName = assemblyName.Name + ".Resources";
                try
                {
                    Type resourceType = assembly.GetType(resourceTypeName, false);
                    if (resourceType != null)
                    {
                        Enable(resourceType);
                        retVal = true;
                    }
                    else
                    {
                        DebugTrace("PLOC: no type {0} found in the assembly {1}",
                            resourceTypeName,
                            assembly.FullName);
                    }
                }
                catch (Exception ex)
                {
                    DebugTrace(ex.ToString());
                }
            }
            else
            {
                DebugTrace("assembly should not be null");
            }

            return retVal;
        }

        /// <summary>
        /// Enables pseudo-localization for the specified RESX managed wrapper class.
        /// </summary>
        /// <param name="resourcesType">Type of the RESX managed wrapper class.</param>
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "ResourceManager", Justification = "Name of property.")]
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "resourceMan", Justification = "Name of field.")]
        public static void Enable(Type resourcesType)
        {
            if (null == resourcesType)
            {
                throw new ArgumentNullException("resourcesType");
            }

            // Get the ResourceManager property
            var resourceManagerProperty = resourcesType.GetProperty("ResourceManager", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (null == resourceManagerProperty)
            {
                throw new NotSupportedException("RESX managed wrapper class does not contain the expected internal/public static ResourceManager property.");
            }

            // Get the ResourceManager value (ensures the resourceMan field gets initialized)
            var resourceManagerValue = resourceManagerProperty.GetValue(null, null) as ResourceManager;
            if (null == resourceManagerValue)
            {
                throw new NotSupportedException("RESX managed wrapper class returned null for the ResourceManager property getter.");
            }

            // Get the resourceMan field
            var resourceManField = resourcesType.GetField("resourceMan", BindingFlags.Static | BindingFlags.NonPublic);
            if (null == resourceManField)
            {
                throw new NotSupportedException("RESX managed wrapper class does not contain the expected private static resourceMan field.");
            }

            // Create a substitute ResourceManager to do the pseudo-localization
            var resourceManSubstitute = new PseudoLocalizerResourceManager(
                _plocPaddingLengthRatio,
                resourceManagerValue.BaseName,
                resourcesType.Assembly);

            // Replace the resourceMan field value
            resourceManField.SetValue(null, resourceManSubstitute);
        }

        /// <summary>
        /// Enables Pseudo-localization for all assemblies that get loaded from the current 
        /// host. Within the assemblie, only types that are of the form {AssemblyName}.Resources 
        /// will get pseudo-localization enabled.
        /// </summary>
        public static void EnableAutoPseudoLocalizationFromHostExecutable()
        {
            if (ShouldPseudoLocalize)
            {
                //set up pseudo-localization for ourselves
                TryEnableAssembly(Assembly.GetExecutingAssembly());

                //set up pseudo-localization for anything that gets loaded later
                AppDomain.CurrentDomain.AssemblyLoad +=
                    new AssemblyLoadEventHandler(OnCurrentDomainAssemblyLoad);
            }
        }

        public static string PseudoLocalizeString(string str)
        {
            return PseudoLocalizerResourceManager.PseudoLocalizeString(
                _plocPaddingLengthRatio, 
                str);
        }

        private static void OnCurrentDomainAssemblyLoad(object sender,
            AssemblyLoadEventArgs args)
        {
            Assembly assembly = args.LoadedAssembly;
            bool isThisMyAssembly;
            if (!assembly.GlobalAssemblyCache)
            {
                isThisMyAssembly = true;
            }
            else if (assembly.FullName.StartsWith("Microsoft.Web",
                StringComparison.OrdinalIgnoreCase))
            {
                isThisMyAssembly = true;
            }
            else
            {
                isThisMyAssembly = false;
            }

            if (isThisMyAssembly)
            {
                TryEnableAssembly(assembly);
            }
        }

        /// <summary>
        /// Class that overrides default ResourceManager behavior by pseudo-localizing its content.
        /// </summary>
        private class PseudoLocalizerResourceManager : ResourceManager
        {
            /// <summary>
            /// Stores a Dictionary for character translations.
            /// </summary>
            private static Dictionary<char, char> _translations = CreateTranslationsMap();

            private static Dictionary<char, char> CreateTranslationsMap()
            {
                // Map standard "English" characters to similar-looking counterparts from other languages
                Dictionary<char, char> translations = new Dictionary<char, char>
                {
                    { 'a', 'ä' },
                    { 'b', 'ƃ' },
                    { 'c', 'č' },
                    { 'd', 'ƌ' },
                    { 'e', 'ë' },
                    { 'f', 'ƒ' },
                    { 'g', 'ğ' },
                    { 'h', 'ħ' },
                    { 'i', 'ï' },
                    { 'j', 'ĵ' },
                    { 'k', 'ƙ' },
                    { 'l', 'ł' },
                    { 'm', 'ɱ' },
                    { 'n', 'ň' },
                    { 'o', 'ö' },
                    { 'p', 'þ' },
                    { 'q', 'ɋ' },
                    { 'r', 'ř' },
                    { 's', 'š' },
                    { 't', 'ŧ' },
                    { 'u', 'ü' },
                    { 'v', 'ṽ' },
                    { 'w', 'ŵ' },
                    { 'x', 'ӿ' },
                    { 'y', 'ŷ' },
                    { 'z', 'ž' },
                    { 'A', 'Ä' },
                    { 'B', 'Ɓ' },
                    { 'C', 'Č' },
                    { 'D', 'Đ' },
                    { 'E', 'Ë' },
                    { 'F', 'Ƒ' },
                    { 'G', 'Ğ' },
                    { 'H', 'Ħ' },
                    { 'I', 'Ï' },
                    { 'J', 'Ĵ' },
                    { 'K', 'Ҟ' },
                    { 'L', 'Ł' },
                    { 'M', 'Ӎ' },
                    { 'N', 'Ň' },
                    { 'O', 'Ö' },
                    { 'P', 'Ҏ' },
                    { 'Q', 'Ǫ' },
                    { 'R', 'Ř' },
                    { 'S', 'Š' },
                    { 'T', 'Ŧ' },
                    { 'U', 'Ü' },
                    { 'V', 'Ṽ' },
                    { 'W', 'Ŵ' },
                    { 'X', 'Ӿ' },
                    { 'Y', 'Ŷ' },
                    { 'Z', 'Ž' },
                };

                return translations;
            }

            private double _paddingLengthRatio;

            /// <summary>
            /// Initializes a new instance of the PseudoLocalizerResourceManager class.
            /// </summary>
            /// <param name="baseName">The root name of the resource file without its extension but including any fully qualified namespace name.</param>
            /// <param name="assembly">The main assembly for the resources.</param>
            public PseudoLocalizerResourceManager(double paddingLengthRatio,
                string baseName,
                Assembly assembly)
                : base(baseName, assembly)
            {
                _paddingLengthRatio = paddingLengthRatio;
            }

            /// <summary>
            /// Returns the value of the specified String resource.
            /// </summary>
            /// <param name="name">The name of the resource to get.</param>
            /// <returns>The value of the resource localized for the caller's current culture settings.</returns>
            public override string GetString(string name)
            {
                return PseudoLocalizeString(base.GetString(name));
            }

            /// <summary>
            /// Gets the value of the String resource localized for the specified culture.
            /// </summary>
            /// <param name="name">The name of the resource to get.</param>
            /// <param name="culture">The CultureInfo object that represents the culture for which the resource is localized.</param>
            /// <returns>The value of the resource localized for the specified culture.</returns>
            public override string GetString(string name, CultureInfo culture)
            {
                return PseudoLocalizeString(base.GetString(name, culture));
            }

            private string PseudoLocalizeString(string str)
            {
                return PseudoLocalizeString(_paddingLengthRatio, str);
            }

            /// <summary>
            /// Pseudo-localizes a string.
            /// </summary>
            /// <param name="str">Input string.</param>
            /// <returns>Pseudo-localized string.</returns>
            internal static string PseudoLocalizeString(double paddingLengthRatio, string str)
            {
                const string minprefix = "[";
                const string minsuffix = "]";

                // Create a new string with the "translated" values of each character
                var translatedChars = new char[str.Length];
                if (IsXamlString(str))
                {
                    DoCaseTranslation(str, translatedChars);
                }
                else
                {
                    DoFunkyCharacterTranslation(str, translatedChars);
                }

                string mungedString = new string(translatedChars);

                int padLengthPerSide = GetPaddingLengthPerSide(
                    str.Length,
                    paddingLengthRatio,
                    minprefix.Length + minsuffix.Length);

                string extraPadding = new string('=', padLengthPerSide);
                string finalString = string.Concat(minprefix, extraPadding, mungedString, extraPadding, minsuffix);

                return finalString;
            }

            private static int GetPaddingLengthPerSide(int originalStringLength, double paddingRatio, int minPadLength)
            {
                int padLengthPerSide;
                double exactTotalPadding = (originalStringLength * paddingRatio);
                padLengthPerSide = (int)(exactTotalPadding + 1) / 2;
                if (padLengthPerSide < 1)
                {
                    padLengthPerSide = 1;
                }

                return padLengthPerSide;
            }

            private static void DoCaseTranslation(string str, char[] translatedChars)
            {
                bool inXamlTag = false;
                for (var i = 0; i < str.Length; i++)
                {
                    var c = str[i];

                    if (inXamlTag)
                    {
                        translatedChars[i] = c;
                        if (c == '>')
                        {
                            inXamlTag = false;
                        }
                    }
                    else
                    {
                        translatedChars[i] = (i % 2) == 0 ? char.ToUpper(c) : char.ToLower(c);
                        if (c == '<' && str.IndexOf('>', i) > -1)
                        {
                            inXamlTag = true;
                        }
                    }
                }
            }

            private static void DoFunkyCharacterTranslation(string str, char[] translatedChars)
            {
                for (var i = 0; i < str.Length; i++)
                {
                    var c = str[i];
                    translatedChars[i] = _translations.ContainsKey(c) ? _translations[c] : c;
                }
            }

            private static bool IsXamlString(string str)
            {
                // check if this string has a '<' followed by a '>' and assume it's a xaml string if so
                int lessThanIndex = str.IndexOf('<');
                if (lessThanIndex > -1)
                {
                    if (str.IndexOf('>', lessThanIndex) > -1)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
#else
    /// <summary>
    /// Dummy class needed to keep the public interface be identical in debug and retail builds
    /// </summary>
    internal static class PseudoLocalizer
    {
        public static bool ShouldPseudoLocalize
        {
            get
            {
                return false;
            }
        }

        public static bool TryEnableAssembly(Assembly assembly)
        {
            return true;
        }

        public static void Enable(Type resourcesType)
        {
        }

        public static void EnableAutoPseudoLocalizationFromHostExecutable()
        {
        }

        public static string PseudoLocalizeString(string str)
        {
            return str;
        }
    }
#endif
}