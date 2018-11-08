// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    public static class XmlExtensions
    {
        internal static bool IsMarkedAsRequiringEncryption(this XElement element)
        {
            return ((bool?)element.Attribute(XmlConstants.RequiresEncryptionAttributeName)).GetValueOrDefault();
        }

        /// <summary>
        /// Marks the provided <see cref="XElement"/> as requiring encryption before being persisted
        /// to storage. Use when implementing <see cref="IAuthenticatedEncryptorDescriptor.ExportToXml"/>.
        /// </summary>
        public static void MarkAsRequiresEncryption(this XElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetAttributeValue(XmlConstants.RequiresEncryptionAttributeName, true);
        }
    }
}
