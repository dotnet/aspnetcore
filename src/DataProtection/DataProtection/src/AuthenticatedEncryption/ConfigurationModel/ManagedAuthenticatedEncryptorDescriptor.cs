// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// A descriptor which can create an authenticated encryption system based upon the
    /// configuration provided by an <see cref="ManagedAuthenticatedEncryptorConfiguration"/> object.
    /// </summary>
    public sealed class ManagedAuthenticatedEncryptorDescriptor : IAuthenticatedEncryptorDescriptor
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ManagedAuthenticatedEncryptorDescriptor"/>.
        /// </summary>
        /// <param name="configuration">The <see cref="ManagedAuthenticatedEncryptorConfiguration"/>.</param>
        /// <param name="masterKey">The master key.</param>
        public ManagedAuthenticatedEncryptorDescriptor(ManagedAuthenticatedEncryptorConfiguration configuration, ISecret masterKey)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (masterKey == null)
            {
                throw new ArgumentNullException(nameof(masterKey));
            }

            Configuration = configuration;
            MasterKey = masterKey;
        }

        internal ISecret MasterKey { get; }

        internal ManagedAuthenticatedEncryptorConfiguration Configuration { get; }

        /// <inheritdoc />
        public XmlSerializedDescriptorInfo ExportToXml()
        {
            // <descriptor>
            //   <!-- managed implementations -->
            //   <encryption algorithm="..." keyLength="..." />
            //   <validation algorithm="..." />
            //   <masterKey>...</masterKey>
            // </descriptor>

            var encryptionElement = new XElement("encryption",
                new XAttribute("algorithm", TypeToFriendlyName(Configuration.EncryptionAlgorithmType)),
                new XAttribute("keyLength", Configuration.EncryptionAlgorithmKeySize));

            var validationElement = new XElement("validation",
                new XAttribute("algorithm", TypeToFriendlyName(Configuration.ValidationAlgorithmType)));

            var rootElement = new XElement("descriptor",
                new XComment(" Algorithms provided by specified SymmetricAlgorithm and KeyedHashAlgorithm "),
                encryptionElement,
                validationElement,
                MasterKey.ToMasterKeyElement());

            return new XmlSerializedDescriptorInfo(rootElement, typeof(ManagedAuthenticatedEncryptorDescriptorDeserializer));
        }

        // Any changes to this method should also be be reflected
        // in ManagedAuthenticatedEncryptorDescriptorDeserializer.FriendlyNameToType.
        private static string TypeToFriendlyName(Type type)
        {
            if (type == typeof(Aes))
            {
                return nameof(Aes);
            }
            else if (type == typeof(HMACSHA1))
            {
                return nameof(HMACSHA1);
            }
            else if (type == typeof(HMACSHA256))
            {
                return nameof(HMACSHA256);
            }
            else if (type == typeof(HMACSHA384))
            {
                return nameof(HMACSHA384);
            }
            else if (type == typeof(HMACSHA512))
            {
                return nameof(HMACSHA512);
            }
            else
            {
                return type.AssemblyQualifiedName!;
            }
        }
    }
}
