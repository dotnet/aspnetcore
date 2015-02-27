// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Xml.Linq;
using Microsoft.Win32;

namespace Microsoft.AspNet.DataProtection.Repositories
{
    /// <summary>
    /// An XML repository backed by the Windows registry.
    /// </summary>
    public class RegistryXmlRepository : IXmlRepository
    {
        public RegistryXmlRepository([NotNull] RegistryKey registryKey)
        {
            RegistryKey = registryKey;
        }

        protected RegistryKey RegistryKey
        {
            get;
            private set;
        }

        public virtual IReadOnlyCollection<XElement> GetAllElements()
        {
            // forces complete enumeration
            return GetAllElementsImpl().ToArray();
        }

        private IEnumerable<XElement> GetAllElementsImpl()
        {
            string[] allValueNames = RegistryKey.GetValueNames();
            foreach (var valueName in allValueNames)
            {
                string thisValue = RegistryKey.GetValue(valueName) as string;
                if (!String.IsNullOrEmpty(thisValue))
                {
                    XDocument document;
                    using (var textReader = new StringReader(thisValue))
                    {
                        document = XDocument.Load(textReader);
                    }

                    // 'yield return' outside the preceding 'using' block so we can release the reader
                    yield return document.Root;
                }
            }
        }

        internal static RegistryXmlRepository GetDefaultRepositoryForHKLMRegistry()
        {
            try
            {
                // Try reading the auto-generated machine key from HKLM
                using (var hklmBaseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                {
                    // TODO: Do we need to change the version number below?
                    string aspnetAutoGenKeysBaseKeyName = String.Format(CultureInfo.InvariantCulture, @"SOFTWARE\Microsoft\ASP.NET\4.0.30319.0\AutoGenKeys\{0}", WindowsIdentity.GetCurrent().User.Value);
                    var aspnetBaseKey = hklmBaseKey.OpenSubKey(aspnetAutoGenKeysBaseKeyName, writable: true);
                    if (aspnetBaseKey == null)
                    {
                        return null; // couldn't find the auto-generated machine key
                    }

                    using (aspnetBaseKey) {
                        // TODO: Remove the ".BETA" moniker.
                        var dataProtectionKey = aspnetBaseKey.OpenSubKey("DataProtection.BETA6", writable: true);
                        if (dataProtectionKey == null)
                        {
                            // TODO: Remove the ".BETA" moniker from here, also.
                            dataProtectionKey = aspnetBaseKey.CreateSubKey("DataProtection.BETA6");
                        }

                        // Once we've opened the HKLM reg key, return a repository which wraps it.
                        return new RegistryXmlRepository(dataProtectionKey);
                    }
                }
            }
            catch
            {
                // swallow all errors; they're not fatal
                return null;
            }
        }

        public virtual void StoreElement([NotNull] XElement element, string friendlyName)
        {
            // We're going to ignore the friendly name for now and just use a GUID.
            StoreElement(element, Guid.NewGuid());
        }

        private void StoreElement(XElement element, Guid id)
        {
            // First, serialize the XElement to a string.
            string serializedString;
            using (var writer = new StringWriter())
            {
                new XDocument(element).Save(writer);
                serializedString = writer.ToString();
            }

            // Technically calls to RegSetValue* and RegGetValue* are atomic, so we don't have to worry about
            // another thread trying to read this value while we're writing it. There's still a small risk of
            // data corruption if power is lost while the registry file is being flushed to the file system,
            // but the window for that should be small enough that we shouldn't have to worry about it.
            string idAsString = id.ToString("D");
            RegistryKey.SetValue(idAsString, serializedString, RegistryValueKind.String);
        }
    }
}
