// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using Microsoft.AspNet.Security.DataProtection.Cng;
using Microsoft.AspNet.Security.DataProtection.KeyManagement;
using Microsoft.AspNet.Security.DataProtection.SafeHandles;

#if !ASPNETCORE50
using System.Security.Principal;
#endif

namespace Microsoft.AspNet.Security.DataProtection.XmlEncryption
{
    /// <summary>
    /// A class that can encrypt XML elements using Windows DPAPI:NG.
    /// </summary>
    public sealed class DpapiNGXmlEncryptor : IXmlEncryptor
    {
        internal static readonly XName DpapiNGEncryptedSecretElementName = XmlKeyManager.KeyManagementXmlNamespace.GetName("dpapiNGEncryptedSecret");

        private readonly NCryptDescriptorHandle _protectionDescriptorHandle;

        public DpapiNGXmlEncryptor()
            : this(GetDefaultProtectionDescriptorString(), DpapiNGProtectionDescriptorFlags.None)
        {
        }

        public DpapiNGXmlEncryptor(string protectionDescriptor, DpapiNGProtectionDescriptorFlags protectionDescriptorFlags = DpapiNGProtectionDescriptorFlags.None)
        {
            if (String.IsNullOrEmpty(protectionDescriptor))
            {
                throw new Exception("TODO: Null or empty.");
            }

            int ntstatus = UnsafeNativeMethods.NCryptCreateProtectionDescriptor(protectionDescriptor, (uint)protectionDescriptorFlags, out _protectionDescriptorHandle);
            UnsafeNativeMethods.ThrowExceptionForNCryptStatus(ntstatus);
            CryptoUtil.AssertSafeHandleIsValid(_protectionDescriptorHandle);
        }

        /// <summary>
        /// Encrypts the specified XML element using Windows DPAPI:NG.
        /// </summary>
        /// <param name="plaintextElement">The plaintext XML element to encrypt. This element is unchanged by the method.</param>
        /// <returns>The encrypted form of the XML element.</returns>
        public XElement Encrypt([NotNull] XElement plaintextElement)
        {
            // First, convert the XML element to a byte[] so that it can be encrypted.
            ProtectedMemoryBlob secret;
            using (var memoryStream = new MemoryStream())
            {
                plaintextElement.Save(memoryStream);

#if !ASPNETCORE50
                // If we're on full desktop CLR, utilize the underlying buffer directly as an optimization.
                byte[] underlyingBuffer = memoryStream.GetBuffer();
                secret = new ProtectedMemoryBlob(new ArraySegment<byte>(underlyingBuffer, 0, checked((int)memoryStream.Length)));
                Array.Clear(underlyingBuffer, 0, underlyingBuffer.Length);
#else
                // Otherwise, need to make a copy of the buffer.
                byte[] clonedBuffer = memoryStream.ToArray();
                secret = new ProtectedMemoryBlob(clonedBuffer);
                Array.Clear(clonedBuffer, 0, clonedBuffer.Length);
#endif
            }

            // <secret decryptor="{TYPE}">
            //   ... base64 data ...
            // </secret>
            byte[] encryptedBytes = DpapiSecretSerializerHelper.ProtectWithDpapiNG(secret, _protectionDescriptorHandle);
            return new XElement(DpapiNGEncryptedSecretElementName,
                new XAttribute("decryptor", typeof(DpapiNGXmlDecryptor).AssemblyQualifiedName),
                new XAttribute("version", 1),
                Convert.ToBase64String(encryptedBytes));
        }

        private static string GetDefaultProtectionDescriptorString()
        {
#if !ASPNETCORE50
            // Creates a SID=... protection descriptor string for the current user.
            // Reminder: DPAPI:NG provides only encryption, not authentication.
            using (WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent())
            {
                // use the SID to create an SDDL string
                return String.Format(CultureInfo.InvariantCulture, "SID={0}", currentIdentity.User.Value);
            }
#else
            throw new NotImplementedException("TODO: Doesn't yet work on Core CLR.");
#endif
        }
    }
}
