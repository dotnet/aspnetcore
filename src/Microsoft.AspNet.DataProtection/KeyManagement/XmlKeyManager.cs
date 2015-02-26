// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.AspNet.Cryptography;
using Microsoft.AspNet.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNet.DataProtection.Repositories;
using Microsoft.AspNet.DataProtection.XmlEncryption;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.DataProtection.KeyManagement
{
    public sealed class XmlKeyManager : IKeyManager
    {
        private const string KEY_MANAGEMENT_XML_NAMESPACE_STRING = "http://www.asp.net/dataProtection/2014";
        internal static readonly XNamespace KeyManagementXmlNamespace = XNamespace.Get(KEY_MANAGEMENT_XML_NAMESPACE_STRING);

        internal static readonly XName ActivationDateElementName = KeyManagementXmlNamespace.GetName("activationDate");
        internal static readonly XName AuthenticatedEncryptorElementName = KeyManagementXmlNamespace.GetName("authenticatedEncryptor");
        internal static readonly XName CreationDateElementName = KeyManagementXmlNamespace.GetName("creationDate");
        internal static readonly XName ExpirationDateElementName = KeyManagementXmlNamespace.GetName("expirationDate");
        internal static readonly XName IdAttributeName = XNamespace.None.GetName("id");
        internal static readonly XName KeyElementName = KeyManagementXmlNamespace.GetName("key");
        internal static readonly XName ReaderAttributeName = XNamespace.None.GetName("reader");
        internal static readonly XName ReasonElementName = KeyManagementXmlNamespace.GetName("reason");
        internal static readonly XName RevocationDateElementName = KeyManagementXmlNamespace.GetName("revocationDate");
        internal static readonly XName RevocationElementName = KeyManagementXmlNamespace.GetName("revocation");
        internal static readonly XName VersionAttributeName = XNamespace.None.GetName("version");

        private readonly IAuthenticatedEncryptorConfigurationFactory _authenticatedEncryptorConfigurationFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITypeActivator _typeActivator;
        private readonly IXmlRepository _xmlRepository;
        private readonly IXmlEncryptor _xmlEncryptor;

        public XmlKeyManager(
            [NotNull] IServiceProvider serviceProvider,
            [NotNull] IAuthenticatedEncryptorConfigurationFactory authenticatedEncryptorConfigurationFactory,
            [NotNull] ITypeActivator typeActivator,
            [NotNull] IXmlRepository xmlRepository,
            [NotNull] IXmlEncryptor xmlEncryptor)
        {
            _serviceProvider = serviceProvider;
            _authenticatedEncryptorConfigurationFactory = authenticatedEncryptorConfigurationFactory;
            _typeActivator = typeActivator;
            _xmlRepository = xmlRepository;
            _xmlEncryptor = xmlEncryptor;
        }

        public IKey CreateNewKey(DateTimeOffset activationDate, DateTimeOffset expirationDate)
        {
            return CreateNewKey(Guid.NewGuid(), DateTimeOffset.UtcNow, activationDate, expirationDate);
        }

        private IKey CreateNewKey(Guid keyId, DateTimeOffset creationDate, DateTimeOffset activationDate, DateTimeOffset expirationDate)
        {
            // <key id="{GUID}" version="1" xmlns="{XMLNS}">
            //   <creationDate>...</creationDate>
            //   <activationDate>...</activationDate>
            //   <expirationDate>...</expirationDate>
            //   <authenticatedEncryptor>
            //     <... parser="{TYPE}" />
            //   </authenticatedEncryptor>
            // </xxx:key>

            // Create the <xxx:authenticatedEncryptor /> element and make sure it's well-formed.
            var encryptorConfiguration = _authenticatedEncryptorConfigurationFactory.CreateNewConfiguration();
            var encryptorElementAsXml = encryptorConfiguration.ToXml(_xmlEncryptor);
            CryptoUtil.Assert(!String.IsNullOrEmpty((string)encryptorElementAsXml.Attribute(ReaderAttributeName)), "!String.IsNullOrEmpty((string)encryptorElementAsXml.Attribute(ParserAttributeName))");

            // Create the <xxx:key /> element.
            var keyElement = new XElement(KeyElementName,
                new XAttribute(IdAttributeName, keyId),
                new XAttribute(VersionAttributeName, 1),
                new XElement(CreationDateElementName, creationDate),
                new XElement(ActivationDateElementName, activationDate),
                new XElement(ExpirationDateElementName, expirationDate),
                new XElement(AuthenticatedEncryptorElementName,
                    encryptorElementAsXml));

            // Persist it to the underlying repository
            string friendlyName = String.Format(CultureInfo.InvariantCulture, "key-{0:D}", keyId);
            _xmlRepository.StoreElement(keyElement, friendlyName);

            // And we're done!
            return new Key(
                keyId: keyId,
                creationDate: creationDate,
                activationDate: activationDate,
                expirationDate: expirationDate,
                encryptorConfiguration: encryptorConfiguration);
        }

        public IReadOnlyCollection<IKey> GetAllKeys()
        {
            var allElements = _xmlRepository.GetAllElements();

            Dictionary<Guid, Key> idToKeyMap = new Dictionary<Guid, Key>();
            HashSet<Guid> revokedKeyIds = null;
            DateTimeOffset? mostRecentMassRevocationDate = null;

            foreach (var element in allElements)
            {
                if (element.Name == KeyElementName)
                {
                    var thisKey = ParseKeyElement(element);
                    if (idToKeyMap.ContainsKey(thisKey.KeyId))
                    {
                        throw CryptoUtil.Fail("TODO: Duplicate key.");
                    }
                    idToKeyMap.Add(thisKey.KeyId, thisKey);
                }
                else if (element.Name == RevocationElementName)
                {
                    object revocationInfo = ParseRevocationElement(element);
                    DateTimeOffset? revocationInfoAsDate = revocationInfo as DateTimeOffset?;
                    if (revocationInfoAsDate != null)
                    {
                        // We're revoking all keys created on or after a specific date.
                        if (!mostRecentMassRevocationDate.HasValue || mostRecentMassRevocationDate < revocationInfoAsDate)
                        {
                            // This new value is the most recent mass revocation date.
                            mostRecentMassRevocationDate = revocationInfoAsDate;
                        }
                    }
                    else
                    {
                        // We're revoking only a specific key
                        if (revokedKeyIds == null)
                        {
                            revokedKeyIds = new HashSet<Guid>();
                        }
                        revokedKeyIds.Add((Guid)revocationInfo);
                    }
                }
                else
                {
                    throw CryptoUtil.Fail("TODO: Unknown element.");
                }
            }

            // Now process all revocations
            if (revokedKeyIds != null || mostRecentMassRevocationDate.HasValue)
            {
                foreach (Key key in idToKeyMap.Values)
                {
                    if ((revokedKeyIds != null && revokedKeyIds.Contains(key.KeyId))
                        || (mostRecentMassRevocationDate.HasValue && mostRecentMassRevocationDate >= key.CreationDate))
                    {
                        key.SetRevoked();
                    }
                }
            }

            // And we're done!
            return idToKeyMap.Values.ToArray();
        }

        private Key ParseKeyElement(XElement keyElement)
        {
            Debug.Assert(keyElement.Name == KeyElementName);

            int version = (int)keyElement.Attribute(VersionAttributeName);
            CryptoUtil.Assert(version == 1, "TODO: version == 1");

            XElement encryptorConfigurationAsXml = keyElement.Element(AuthenticatedEncryptorElementName).Elements().Single();
            string encryptorConfigurationParserTypeName = (string)encryptorConfigurationAsXml.Attribute(ReaderAttributeName);
            Type encryptorConfigurationParserType = Type.GetType(encryptorConfigurationParserTypeName, throwOnError: true);
            CryptoUtil.Assert(typeof(IAuthenticatedEncryptorConfigurationXmlReader).IsAssignableFrom(encryptorConfigurationParserType),
                "TODO: typeof(IAuthenticatedEncryptorConfigurationXmlReader).IsAssignableFrom(encryptorConfigurationParserType)");

            var parser = (IAuthenticatedEncryptorConfigurationXmlReader)_typeActivator.CreateInstance(_serviceProvider, encryptorConfigurationParserType);
            var encryptorConfiguration = parser.FromXml(encryptorConfigurationAsXml);

            Guid keyId = (Guid)keyElement.Attribute(IdAttributeName);
            DateTimeOffset creationDate = (DateTimeOffset)keyElement.Element(CreationDateElementName);
            DateTimeOffset activationDate = (DateTimeOffset)keyElement.Element(ActivationDateElementName);
            DateTimeOffset expirationDate = (DateTimeOffset)keyElement.Element(ExpirationDateElementName);

            return new Key(
                keyId: keyId,
                creationDate: creationDate,
                activationDate: activationDate,
                expirationDate: expirationDate,
                encryptorConfiguration: encryptorConfiguration);
        }

        // returns a Guid (for specific keys) or a DateTimeOffset (for all keys created on or before a specific date)
        private object ParseRevocationElement(XElement revocationElement)
        {
            Debug.Assert(revocationElement.Name == RevocationElementName);

            string keyIdAsString = revocationElement.Element(KeyElementName).Attribute(IdAttributeName).Value;
            if (keyIdAsString == "*")
            {
                // all keys
                return (DateTimeOffset)revocationElement.Element(RevocationDateElementName);
            }
            else
            {
                // only one key
                return new Guid(keyIdAsString);
            }
        }

        public void RevokeAllKeys(DateTimeOffset revocationDate, string reason = null)
        {
            // <revocation version="1" xmlns="{XMLNS}">
            //   <revocationDate>...</revocationDate>
            //   <key id="*" />
            //   <reason>...</reason>
            // </revocation>

            var revocationElement = new XElement(RevocationElementName,
                new XAttribute(VersionAttributeName, 1),
                new XElement(RevocationDateElementName, revocationDate),
                new XElement(KeyElementName,
                    new XAttribute(IdAttributeName, "*")),
                new XElement(ReasonElementName, reason));

            // Persist it to the underlying repository
            string friendlyName = String.Format(CultureInfo.InvariantCulture, "revocation-{0:X16}", (ulong)revocationDate.UtcTicks);
            _xmlRepository.StoreElement(revocationElement, friendlyName);
        }

        public void RevokeKey(Guid keyId, string reason = null)
        {
            RevokeSingleKey(keyId, DateTimeOffset.UtcNow, reason);
        }

        private void RevokeSingleKey(Guid keyId, DateTimeOffset utcNow, string reason)
        {
            // <revocation version="1" xmlns="{XMLNS}">
            //   <revocationDate>...</revocationDate>
            //   <key id="{GUID}" />
            //   <reason>...</reason>
            // </revocation>

            var revocationElement = new XElement(RevocationElementName,
                new XAttribute(VersionAttributeName, 1),
                new XElement(RevocationDateElementName, utcNow),
                new XElement(KeyElementName,
                    new XAttribute(IdAttributeName, keyId)),
                new XElement(ReasonElementName, reason));

            // Persist it to the underlying repository
            string friendlyName = String.Format(CultureInfo.InvariantCulture, "revocation-{0:D}", keyId);
            _xmlRepository.StoreElement(revocationElement, friendlyName);
        }
    }
}
