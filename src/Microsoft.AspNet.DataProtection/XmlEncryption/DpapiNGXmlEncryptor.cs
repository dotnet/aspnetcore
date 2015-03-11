// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Security.Principal;
using System.Xml.Linq;
using Microsoft.AspNet.Cryptography;
using Microsoft.AspNet.Cryptography.SafeHandles;
using Microsoft.AspNet.DataProtection.Cng;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.DataProtection.XmlEncryption
{
    /// <summary>
    /// A class that can encrypt XML elements using Windows DPAPI:NG.
    /// </summary>
    /// <remarks>
    /// This API is only supported on Windows 8 / Windows Server 2012 and higher.
    /// </remarks>
    public sealed class DpapiNGXmlEncryptor : IXmlEncryptor
    {
        private readonly ILogger _logger;
        private readonly NCryptDescriptorHandle _protectionDescriptorHandle;

        /// <summary>
        /// Creates a new instance of a <see cref="DpapiNGXmlEncryptor"/>.
        /// </summary>
        /// <param name="protectionDescriptorRule">The rule string from which to create the protection descriptor.</param>
        /// <param name="flags">Flags controlling the creation of the protection descriptor.</param>
        public DpapiNGXmlEncryptor([NotNull] string protectionDescriptorRule, DpapiNGProtectionDescriptorFlags flags)
            : this(protectionDescriptorRule, flags, services: null)
        {
        }

        /// <summary>
        /// Creates a new instance of a <see cref="DpapiNGXmlEncryptor"/>.
        /// </summary>
        /// <param name="protectionDescriptorRule">The rule string from which to create the protection descriptor.</param>
        /// <param name="flags">Flags controlling the creation of the protection descriptor.</param>
        /// <param name="services">An optional <see cref="IServiceProvider"/> to provide ancillary services.</param>
        public DpapiNGXmlEncryptor([NotNull] string protectionDescriptorRule, DpapiNGProtectionDescriptorFlags flags, IServiceProvider services)
        {
            CryptoUtil.AssertPlatformIsWindows8OrLater();

            int ntstatus = UnsafeNativeMethods.NCryptCreateProtectionDescriptor(protectionDescriptorRule, (uint)flags, out _protectionDescriptorHandle);
            UnsafeNativeMethods.ThrowExceptionForNCryptStatus(ntstatus);
            CryptoUtil.AssertSafeHandleIsValid(_protectionDescriptorHandle);

            _logger = services.GetLogger<DpapiNGXmlEncryptor>();
        }

        /// <summary>
        /// Encrypts the specified <see cref="XElement"/>.
        /// </summary>
        /// <param name="plaintextElement">The plaintext to encrypt.</param>
        /// <returns>
        /// An <see cref="EncryptedXmlInfo"/> that contains the encrypted value of
        /// <paramref name="plaintextElement"/> along with information about how to
        /// decrypt it.
        /// </returns>
        public EncryptedXmlInfo Encrypt([NotNull] XElement plaintextElement)
        {
            string protectionDescriptorRuleString = _protectionDescriptorHandle.GetProtectionDescriptorRuleString();
            if (_logger.IsVerboseLevelEnabled())
            {
                _logger.LogVerbose("Encrypting to Windows DPAPI-NG using protection descriptor '{0}'.", protectionDescriptorRuleString);
            }

            // Convert the XML element to a binary secret so that it can be run through DPAPI
            byte[] cngDpapiEncryptedData;
            try
            {
                using (Secret plaintextElementAsSecret = plaintextElement.ToSecret())
                {
                    cngDpapiEncryptedData = DpapiSecretSerializerHelper.ProtectWithDpapiNG(plaintextElementAsSecret, _protectionDescriptorHandle);
                }
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorLevelEnabled())
                {
                    _logger.LogError(ex, "An error occurred while encrypting to Windows DPAPI-NG.");
                }
                throw;
            }

            // <encryptedKey>
            //   <!-- This key is encrypted with {provider}. -->
            //   <!-- rule string -->
            //   <value>{base64}</value>
            // </encryptedKey>
            
            var element = new XElement("encryptedKey",
                new XComment(" This key is encrypted with Windows DPAPI-NG. "),
                new XComment(" Rule: " + protectionDescriptorRuleString + " "),
                new XElement("value",
                    Convert.ToBase64String(cngDpapiEncryptedData)));

            return new EncryptedXmlInfo(element, typeof(DpapiNGXmlDecryptor));
        }

        /// <summary>
        /// Creates a rule string tied to the current Windows user and which is transferrable
        /// across machines (backed up in AD).
        /// </summary>
        internal static string GetDefaultProtectionDescriptorString()
        {
            CryptoUtil.AssertPlatformIsWindows8OrLater();

            // Creates a SID=... protection descriptor string for the current user.
            // Reminder: DPAPI:NG provides only encryption, not authentication.
            using (WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent())
            {
                // use the SID to create an SDDL string
                return String.Format(CultureInfo.InvariantCulture, "SID={0}", currentIdentity.User.Value);
            }
        }
    }
}
