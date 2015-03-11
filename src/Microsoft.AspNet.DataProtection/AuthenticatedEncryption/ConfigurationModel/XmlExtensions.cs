// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel
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
        public static void MarkAsRequiresEncryption([NotNull] this XElement element)
        {
            element.SetAttributeValue(XmlConstants.RequiresEncryptionAttributeName, true);
        }
    }
}
