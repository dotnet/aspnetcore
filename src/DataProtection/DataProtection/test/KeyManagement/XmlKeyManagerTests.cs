// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.Internal;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement
{
    public class XmlKeyManagerTests
    {
        private static readonly XElement serializedDescriptor = XElement.Parse(@"
            <theElement>
              <secret enc:requiresEncryption='true' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                <![CDATA[This is a secret value.]]>
              </secret>
            </theElement>");

        [Fact]
        public void Ctor_WithoutEncryptorOrRepository_UsesFallback()
        {
            // Arrange
            var options = Options.Create(new KeyManagementOptions()
            {
                AuthenticatedEncryptorConfiguration = new Mock<AlgorithmConfiguration>().Object,
                XmlRepository = null,
                XmlEncryptor = null
            });

            // Act
            var keyManager = new XmlKeyManager(options, SimpleActivator.DefaultWithoutServices, NullLoggerFactory.Instance);

            // Assert
            Assert.NotNull(keyManager.KeyRepository);

            if (OSVersionUtil.IsWindows())
            {
                Assert.NotNull(keyManager.KeyEncryptor);
            }
        }

        [Fact]
        public void Ctor_WithEncryptorButNoRepository_IgnoresFallback_FailsWithServiceNotFound()
        {
            // Arrange
            var options = Options.Create(new KeyManagementOptions()
            {
                AuthenticatedEncryptorConfiguration = new Mock<AlgorithmConfiguration>().Object,
                XmlRepository = null,
                XmlEncryptor = new Mock<IXmlEncryptor>().Object
            });

            // Act & assert - we don't care about exception type, only exception message
            Exception ex = Assert.ThrowsAny<Exception>(
                () => new XmlKeyManager(options, SimpleActivator.DefaultWithoutServices, NullLoggerFactory.Instance));
            Assert.Contains("IXmlRepository", ex.Message);
        }

        [Fact]
        public void CreateNewKey_Internal_NoEscrowOrEncryption()
        {
            // Constants
            var creationDate = new DateTimeOffset(2014, 01, 01, 0, 0, 0, TimeSpan.Zero);
            var activationDate = new DateTimeOffset(2014, 02, 01, 0, 0, 0, TimeSpan.Zero);
            var expirationDate = new DateTimeOffset(2014, 03, 01, 0, 0, 0, TimeSpan.Zero);
            var keyId = new Guid("3d6d01fd-c0e7-44ae-82dd-013b996b4093");

            // Arrange
            XElement elementStoredInRepository = null;
            string friendlyNameStoredInRepository = null;
            var expectedAuthenticatedEncryptor = new Mock<IAuthenticatedEncryptor>().Object;
            var mockDescriptor = new Mock<IAuthenticatedEncryptorDescriptor>();
            mockDescriptor.Setup(o => o.ExportToXml()).Returns(new XmlSerializedDescriptorInfo(serializedDescriptor, typeof(MyDeserializer)));
            var expectedDescriptor = mockDescriptor.Object;
            var testEncryptorFactory = new TestEncryptorFactory(expectedDescriptor, expectedAuthenticatedEncryptor);
            var mockConfiguration = new Mock<AlgorithmConfiguration>();
            mockConfiguration.Setup(o => o.CreateNewDescriptor()).Returns(expectedDescriptor);
            var mockXmlRepository = new Mock<IXmlRepository>();
            mockXmlRepository
                .Setup(o => o.StoreElement(It.IsAny<XElement>(), It.IsAny<string>()))
                .Callback<XElement, string>((el, friendlyName) =>
                {
                    elementStoredInRepository = el;
                    friendlyNameStoredInRepository = friendlyName;
                });
            var options = Options.Create(new KeyManagementOptions()
            {
                AuthenticatedEncryptorConfiguration = mockConfiguration.Object,
                XmlRepository = mockXmlRepository.Object,
                XmlEncryptor = null
            });
            options.Value.AuthenticatedEncryptorFactories.Add(testEncryptorFactory);

            var keyManager = new XmlKeyManager(options, SimpleActivator.DefaultWithoutServices, NullLoggerFactory.Instance);

            // Act & assert

            // The cancellation token should not already be fired
            var firstCancellationToken = keyManager.GetCacheExpirationToken();
            Assert.False(firstCancellationToken.IsCancellationRequested);

            // After the call to CreateNewKey, the first CT should be fired,
            // and we should've gotten a new CT.
            var newKey = ((IInternalXmlKeyManager)keyManager).CreateNewKey(
                keyId: keyId,
                creationDate: creationDate,
                activationDate: activationDate,
                expirationDate: expirationDate);
            var secondCancellationToken = keyManager.GetCacheExpirationToken();
            Assert.True(firstCancellationToken.IsCancellationRequested);
            Assert.False(secondCancellationToken.IsCancellationRequested);

            // Does the IKey have the properties we requested?
            Assert.Equal(keyId, newKey.KeyId);
            Assert.Equal(creationDate, newKey.CreationDate);
            Assert.Equal(activationDate, newKey.ActivationDate);
            Assert.Equal(expirationDate, newKey.ExpirationDate);
            Assert.Same(expectedDescriptor, newKey.Descriptor);
            Assert.False(newKey.IsRevoked);
            Assert.Same(expectedAuthenticatedEncryptor, testEncryptorFactory.CreateEncryptorInstance(newKey));

            // Finally, was the correct element stored in the repository?
            string expectedXml = string.Format(@"
                <key id='3d6d01fd-c0e7-44ae-82dd-013b996b4093' version='1' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                  {1}
                  {2}
                  {3}
                  <descriptor deserializerType='{0}'>
                    <theElement>
                      <secret enc:requiresEncryption='true'>
                        <![CDATA[This is a secret value.]]>
                      </secret>
                    </theElement>
                  </descriptor>
                </key>",
                typeof(MyDeserializer).AssemblyQualifiedName,
                new XElement("creationDate", creationDate),
                new XElement("activationDate", activationDate),
                new XElement("expirationDate", expirationDate));
            XmlAssert.Equal(expectedXml, elementStoredInRepository);
            Assert.Equal("key-3d6d01fd-c0e7-44ae-82dd-013b996b4093", friendlyNameStoredInRepository);
        }

        [Fact]
        public void CreateNewKey_Internal_WithEscrowAndEncryption()
        {
            // Constants
            var creationDate = new DateTimeOffset(2014, 01, 01, 0, 0, 0, TimeSpan.Zero);
            var activationDate = new DateTimeOffset(2014, 02, 01, 0, 0, 0, TimeSpan.Zero);
            var expirationDate = new DateTimeOffset(2014, 03, 01, 0, 0, 0, TimeSpan.Zero);
            var keyId = new Guid("3d6d01fd-c0e7-44ae-82dd-013b996b4093");

            // Arrange
            XElement elementStoredInEscrow = null;
            Guid? keyIdStoredInEscrow = null;
            XElement elementStoredInRepository = null;
            string friendlyNameStoredInRepository = null;
            var expectedAuthenticatedEncryptor = new Mock<IAuthenticatedEncryptor>().Object;
            var mockDescriptor = new Mock<IAuthenticatedEncryptorDescriptor>();
            mockDescriptor.Setup(o => o.ExportToXml()).Returns(new XmlSerializedDescriptorInfo(serializedDescriptor, typeof(MyDeserializer)));
            var expectedDescriptor = mockDescriptor.Object;
            var testEncryptorFactory = new TestEncryptorFactory(expectedDescriptor, expectedAuthenticatedEncryptor);
            var mockConfiguration = new Mock<AlgorithmConfiguration>();
            mockConfiguration.Setup(o => o.CreateNewDescriptor()).Returns(expectedDescriptor);
            var mockXmlRepository = new Mock<IXmlRepository>();
            mockXmlRepository
                .Setup(o => o.StoreElement(It.IsAny<XElement>(), It.IsAny<string>()))
                .Callback<XElement, string>((el, friendlyName) =>
                {
                    elementStoredInRepository = el;
                    friendlyNameStoredInRepository = friendlyName;
                });
            var mockKeyEscrow = new Mock<IKeyEscrowSink>();
            mockKeyEscrow
                .Setup(o => o.Store(It.IsAny<Guid>(), It.IsAny<XElement>()))
                .Callback<Guid, XElement>((innerKeyId, el) =>
                {
                    keyIdStoredInEscrow = innerKeyId;
                    elementStoredInEscrow = el;
                });

            var options = Options.Create(new KeyManagementOptions()
            {
                AuthenticatedEncryptorConfiguration = mockConfiguration.Object,
                XmlRepository = mockXmlRepository.Object,
                XmlEncryptor = new NullXmlEncryptor()
            });
            options.Value.AuthenticatedEncryptorFactories.Add(testEncryptorFactory);
            options.Value.KeyEscrowSinks.Add(mockKeyEscrow.Object);
            var keyManager = new XmlKeyManager(options, SimpleActivator.DefaultWithoutServices, NullLoggerFactory.Instance);

            // Act & assert

            // The cancellation token should not already be fired
            var firstCancellationToken = keyManager.GetCacheExpirationToken();
            Assert.False(firstCancellationToken.IsCancellationRequested);

            // After the call to CreateNewKey, the first CT should be fired,
            // and we should've gotten a new CT.
            var newKey = ((IInternalXmlKeyManager)keyManager).CreateNewKey(
                keyId: keyId,
                creationDate: creationDate,
                activationDate: activationDate,
                expirationDate: expirationDate);
            var secondCancellationToken = keyManager.GetCacheExpirationToken();
            Assert.True(firstCancellationToken.IsCancellationRequested);
            Assert.False(secondCancellationToken.IsCancellationRequested);

            // Does the IKey have the properties we requested?
            Assert.Equal(keyId, newKey.KeyId);
            Assert.Equal(creationDate, newKey.CreationDate);
            Assert.Equal(activationDate, newKey.ActivationDate);
            Assert.Equal(expirationDate, newKey.ExpirationDate);
            Assert.Same(expectedDescriptor, newKey.Descriptor);
            Assert.False(newKey.IsRevoked);
            Assert.Same(expectedAuthenticatedEncryptor, testEncryptorFactory.CreateEncryptorInstance(newKey));

            // Was the correct element stored in escrow?
            // This should not have gone through the encryptor.
            string expectedEscrowXml = string.Format(@"
                <key id='3d6d01fd-c0e7-44ae-82dd-013b996b4093' version='1' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                  {1}
                  {2}
                  {3}
                  <descriptor deserializerType='{0}'>
                    <theElement>
                      <secret enc:requiresEncryption='true'>
                        <![CDATA[This is a secret value.]]>
                      </secret>
                    </theElement>
                  </descriptor>
                </key>",
                typeof(MyDeserializer).AssemblyQualifiedName,
                new XElement("creationDate", creationDate),
                new XElement("activationDate", activationDate),
                new XElement("expirationDate", expirationDate));
            XmlAssert.Equal(expectedEscrowXml, elementStoredInEscrow);
            Assert.Equal(keyId, keyIdStoredInEscrow.Value);

            // Finally, was the correct element stored in the repository?
            // This should have gone through the encryptor (which we set to be the null encryptor in this test)
            string expectedRepositoryXml = String.Format(@"
                <key id='3d6d01fd-c0e7-44ae-82dd-013b996b4093' version='1' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                  {2}
                  {3}
                  {4}
                  <descriptor deserializerType='{0}'>
                    <theElement>
                      <enc:encryptedSecret decryptorType='{1}'>
                        <unencryptedKey>
                          <secret enc:requiresEncryption='true'>
                            <![CDATA[This is a secret value.]]>
                          </secret>
                        </unencryptedKey>
                      </enc:encryptedSecret>
                    </theElement>
                  </descriptor>
                </key>",
                typeof(MyDeserializer).AssemblyQualifiedName,
                typeof(NullXmlDecryptor).AssemblyQualifiedName,
                new XElement("creationDate", creationDate),
                new XElement("activationDate", activationDate),
                new XElement("expirationDate", expirationDate));
            XmlAssert.Equal(expectedRepositoryXml, elementStoredInRepository);
            Assert.Equal("key-3d6d01fd-c0e7-44ae-82dd-013b996b4093", friendlyNameStoredInRepository);
        }

        [Fact]
        public void CreateNewKey_CallsInternalManager()
        {
            // Arrange
            DateTimeOffset minCreationDate = DateTimeOffset.UtcNow;
            DateTimeOffset? actualCreationDate = null;
            DateTimeOffset activationDate = minCreationDate + TimeSpan.FromDays(7);
            DateTimeOffset expirationDate = activationDate.AddMonths(1);
            var mockInternalKeyManager = new Mock<IInternalXmlKeyManager>();
            mockInternalKeyManager
                .Setup(o => o.CreateNewKey(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), activationDate, expirationDate))
                .Callback<Guid, DateTimeOffset, DateTimeOffset, DateTimeOffset>((innerKeyId, innerCreationDate, innerActivationDate, innerExpirationDate) =>
                {
                    actualCreationDate = innerCreationDate;
                });

            var options = Options.Create(new KeyManagementOptions()
            {
                AuthenticatedEncryptorConfiguration = new Mock<AlgorithmConfiguration>().Object,
                XmlRepository = new Mock<IXmlRepository>().Object,
                XmlEncryptor = null
            });
            var keyManager = new XmlKeyManager(options, SimpleActivator.DefaultWithoutServices, NullLoggerFactory.Instance, mockInternalKeyManager.Object);

            // Act
            keyManager.CreateNewKey(activationDate, expirationDate);

            // Assert
            Assert.InRange(actualCreationDate.Value, minCreationDate, DateTimeOffset.UtcNow);
        }

        [Fact]
        public void GetAllKeys_Empty()
        {
            // Arrange
            const string xml = @"<root />";
            var activator = new Mock<IActivator>().Object;

            // Act
            var keys = RunGetAllKeysCore(xml, activator);

            // Assert
            Assert.Equal(0, keys.Count);
        }

        [Fact]
        public void GetAllKeys_IgnoresUnknownElements()
        {
            // Arrange
            const string xml = @"
                <root>
                  <key id='62a72ad9-42d7-4e97-b3fa-05bad5d53d33' version='1'>
                    <creationDate>2015-01-01T00:00:00Z</creationDate>
                    <activationDate>2015-02-01T00:00:00Z</activationDate>
                    <expirationDate>2015-03-01T00:00:00Z</expirationDate>
                    <descriptor deserializerType='deserializer-A'>
                      <elementA />
                    </descriptor>
                  </key>
                  <unknown>
                    <![CDATA[Unknown elements are ignored.]]>
                  </unknown>
                  <key id='041be4c0-52d7-48b4-8d32-f8c0ff315459' version='1'>
                    <creationDate>2015-04-01T00:00:00Z</creationDate>
                    <activationDate>2015-05-01T00:00:00Z</activationDate>
                    <expirationDate>2015-06-01T00:00:00Z</expirationDate>
                    <descriptor deserializerType='deserializer-B'>
                      <elementB />
                    </descriptor>
                  </key>
                </root>";

            var descriptorA = new Mock<IAuthenticatedEncryptorDescriptor>().Object;
            var descriptorB = new Mock<IAuthenticatedEncryptorDescriptor>().Object;
            var mockActivator = new Mock<IActivator>();
            mockActivator.ReturnDescriptorGivenDeserializerTypeNameAndInput("deserializer-A", "<elementA />", descriptorA);
            mockActivator.ReturnDescriptorGivenDeserializerTypeNameAndInput("deserializer-B", "<elementB />", descriptorB);

            // Act
            var keys = RunGetAllKeysCore(xml, mockActivator.Object).ToArray();

            // Assert
            Assert.Equal(2, keys.Length);
            Assert.Equal(new Guid("62a72ad9-42d7-4e97-b3fa-05bad5d53d33"), keys[0].KeyId);
            Assert.Equal(XmlConvert.ToDateTimeOffset("2015-01-01T00:00:00Z"), keys[0].CreationDate);
            Assert.Equal(XmlConvert.ToDateTimeOffset("2015-02-01T00:00:00Z"), keys[0].ActivationDate);
            Assert.Equal(XmlConvert.ToDateTimeOffset("2015-03-01T00:00:00Z"), keys[0].ExpirationDate);
            Assert.False(keys[0].IsRevoked);
            Assert.Same(descriptorA, keys[0].Descriptor);
            Assert.Equal(new Guid("041be4c0-52d7-48b4-8d32-f8c0ff315459"), keys[1].KeyId);
            Assert.Equal(XmlConvert.ToDateTimeOffset("2015-04-01T00:00:00Z"), keys[1].CreationDate);
            Assert.Equal(XmlConvert.ToDateTimeOffset("2015-05-01T00:00:00Z"), keys[1].ActivationDate);
            Assert.Equal(XmlConvert.ToDateTimeOffset("2015-06-01T00:00:00Z"), keys[1].ExpirationDate);
            Assert.False(keys[1].IsRevoked);
            Assert.Same(descriptorB, keys[1].Descriptor);
        }

        [Fact]
        public void GetAllKeys_UnderstandsRevocations()
        {
            // Arrange
            const string xml = @"
                <root>
                  <key id='67f9cdea-83ba-41ed-b160-2b1d0ea30251' version='1'>
                    <creationDate>2015-01-01T00:00:00Z</creationDate>
                    <activationDate>2015-02-01T00:00:00Z</activationDate>
                    <expirationDate>2015-03-01T00:00:00Z</expirationDate>
                    <descriptor deserializerType='theDeserializer'>
                      <node />
                    </descriptor>
                  </key>
                  <key id='0cf83742-d175-42a8-94b5-1ec049b354c3' version='1'>
                    <creationDate>2016-01-01T00:00:00Z</creationDate>
                    <activationDate>2016-02-01T00:00:00Z</activationDate>
                    <expirationDate>2016-03-01T00:00:00Z</expirationDate>
                    <descriptor deserializerType='theDeserializer'>
                      <node />
                    </descriptor>
                  </key>
                  <key id='21580ac4-c83a-493c-bde6-29a1cc97ca0f' version='1'>
                    <creationDate>2017-01-01T00:00:00Z</creationDate>
                    <activationDate>2017-02-01T00:00:00Z</activationDate>
                    <expirationDate>2017-03-01T00:00:00Z</expirationDate>
                    <descriptor deserializerType='theDeserializer'>
                      <node />
                    </descriptor>
                  </key>
                  <key id='6bd14f12-0bb8-4822-91d7-04b360de0497' version='1'>
                    <creationDate>2018-01-01T00:00:00Z</creationDate>
                    <activationDate>2018-02-01T00:00:00Z</activationDate>
                    <expirationDate>2018-03-01T00:00:00Z</expirationDate>
                    <descriptor deserializerType='theDeserializer'>
                      <node />
                    </descriptor>
                  </key>
                  <revocation version='1'>
                    <!-- The below will revoke no keys. -->
                    <revocationDate>2014-01-01T00:00:00Z</revocationDate>
                    <key id='*' />
                  </revocation>
                  <revocation version='1'>
                    <!-- The below will revoke the first two keys. -->
                    <revocationDate>2017-01-01T00:00:00Z</revocationDate>
                    <key id='*' />
                  </revocation>
                  <revocation version='1'>
                    <!-- The below will revoke only the last key. -->
                    <revocationDate>2020-01-01T00:00:00Z</revocationDate>
                    <key id='6bd14f12-0bb8-4822-91d7-04b360de0497' />
                  </revocation>
                </root>";

            var mockActivator = new Mock<IActivator>();
            mockActivator.ReturnDescriptorGivenDeserializerTypeNameAndInput("theDeserializer", "<node />", new Mock<IAuthenticatedEncryptorDescriptor>().Object);

            // Act
            var keys = RunGetAllKeysCore(xml, mockActivator.Object).ToArray();

            // Assert
            Assert.Equal(4, keys.Length);
            Assert.Equal(new Guid("67f9cdea-83ba-41ed-b160-2b1d0ea30251"), keys[0].KeyId);
            Assert.True(keys[0].IsRevoked);
            Assert.Equal(new Guid("0cf83742-d175-42a8-94b5-1ec049b354c3"), keys[1].KeyId);
            Assert.True(keys[1].IsRevoked);
            Assert.Equal(new Guid("21580ac4-c83a-493c-bde6-29a1cc97ca0f"), keys[2].KeyId);
            Assert.False(keys[2].IsRevoked);
            Assert.Equal(new Guid("6bd14f12-0bb8-4822-91d7-04b360de0497"), keys[3].KeyId);
            Assert.True(keys[3].IsRevoked);
        }

        [Fact]
        public void GetAllKeys_PerformsDecryption()
        {
            // Arrange
            const string xml = @"
                <root xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
                  <key id='09712588-ba68-438a-a5ee-fe842b3453b2' version='1'>
                    <creationDate>2015-01-01T00:00:00Z</creationDate>
                    <activationDate>2015-02-01T00:00:00Z</activationDate>
                    <expirationDate>2015-03-01T00:00:00Z</expirationDate>
                    <descriptor deserializerType='theDeserializer'>
                      <enc:encryptedSecret decryptorType='theDecryptor'>
                        <node xmlns='private' />
                      </enc:encryptedSecret>
                    </descriptor>
                  </key>
                </root>";

            var expectedDescriptor = new Mock<IAuthenticatedEncryptorDescriptor>().Object;
            var mockActivator = new Mock<IActivator>();
            mockActivator.ReturnDecryptedElementGivenDecryptorTypeNameAndInput("theDecryptor", "<node xmlns='private' />", "<decryptedNode />");
            mockActivator.ReturnDescriptorGivenDeserializerTypeNameAndInput("theDeserializer", "<decryptedNode />", expectedDescriptor);

            // Act
            var keys = RunGetAllKeysCore(xml, mockActivator.Object).ToArray();

            // Assert
            Assert.Single(keys);
            Assert.Equal(new Guid("09712588-ba68-438a-a5ee-fe842b3453b2"), keys[0].KeyId);
            Assert.Same(expectedDescriptor, keys[0].Descriptor);
        }

        [Fact]
        public void GetAllKeys_SwallowsKeyDeserializationErrors()
        {
            // Arrange
            const string xml = @"
                <root>
                  <!-- The below key will throw an exception when deserializing. -->
                  <key id='78cd498e-9375-4e55-ac0d-d79527ecd09d' version='1'>
                    <creationDate>2015-01-01T00:00:00Z</creationDate>
                    <activationDate>2015-02-01T00:00:00Z</activationDate>
                    <expirationDate>NOT A VALID DATE</expirationDate>
                    <descriptor deserializerType='badDeserializer'>
                      <node />
                    </descriptor>
                  </key>
                  <!-- The below key will deserialize properly. -->
                  <key id='49c0cda9-0232-4d8c-a541-de20cc5a73d6' version='1'>
                    <creationDate>2015-01-01T00:00:00Z</creationDate>
                    <activationDate>2015-02-01T00:00:00Z</activationDate>
                    <expirationDate>2015-03-01T00:00:00Z</expirationDate>
                    <descriptor deserializerType='goodDeserializer'>
                      <node xmlns='private' />
                    </descriptor>
                  </key>
                </root>";

            var expectedDescriptor = new Mock<IAuthenticatedEncryptorDescriptor>().Object;
            var mockActivator = new Mock<IActivator>();
            mockActivator.ReturnDescriptorGivenDeserializerTypeNameAndInput("goodDeserializer", "<node xmlns='private' />", expectedDescriptor);

            // Act
            var keys = RunGetAllKeysCore(xml, mockActivator.Object).ToArray();

            // Assert
            Assert.Single(keys);
            Assert.Equal(new Guid("49c0cda9-0232-4d8c-a541-de20cc5a73d6"), keys[0].KeyId);
            Assert.Same(expectedDescriptor, keys[0].Descriptor);
        }

        [Fact]
        public void GetAllKeys_WithKeyDeserializationError_LogLevelDebug_DoesNotWriteSensitiveInformation()
        {
            // Arrange
            const string xml = @"
                <root>
                  <!-- The below key will throw an exception when deserializing. -->
                  <key id='78cd498e-9375-4e55-ac0d-d79527ecd09d' version='1'>
                    <creationDate>2015-01-01T00:00:00Z</creationDate>
                    <activationDate>2015-02-01T00:00:00Z</activationDate>
                    <expirationDate>NOT A VALID DATE</expirationDate>
                    <!-- Secret information: 1A2B3C4D -->
                  </key>
                </root>";

            var loggerFactory = new StringLoggerFactory(LogLevel.Debug);

            // Act
            RunGetAllKeysCore(xml, new Mock<IActivator>().Object, loggerFactory).ToArray();

            // Assert
            Assert.False(loggerFactory.ToString().Contains("1A2B3C4D"), "The secret '1A2B3C4D' should not have been logged.");
        }

        [Fact]
        public void GetAllKeys_WithKeyDeserializationError_LogLevelTrace_WritesSensitiveInformation()
        {
            // Arrange
            const string xml = @"
                <root>
                  <!-- The below key will throw an exception when deserializing. -->
                  <key id='78cd498e-9375-4e55-ac0d-d79527ecd09d' version='1'>
                    <creationDate>2015-01-01T00:00:00Z</creationDate>
                    <activationDate>2015-02-01T00:00:00Z</activationDate>
                    <expirationDate>NOT A VALID DATE</expirationDate>
                    <!-- Secret information: 1A2B3C4D -->
                  </key>
                </root>";

            var loggerFactory = new StringLoggerFactory(LogLevel.Trace);

            // Act
            RunGetAllKeysCore(xml, new Mock<IActivator>().Object, loggerFactory).ToArray();

            // Assert
            Assert.True(loggerFactory.ToString().Contains("1A2B3C4D"), "The secret '1A2B3C4D' should have been logged.");
        }

        [Fact]
        public void GetAllKeys_SurfacesRevocationDeserializationErrors()
        {
            // Arrange
            const string xml = @"
                <root>
                  <revocation version='1'>
                    <revocationDate>2015-01-01T00:00:00Z</revocationDate>
                    <key id='{invalid}' />
                  </revocation>
                </root>";

            // Act & assert
            // Bad GUID will lead to FormatException
            Assert.Throws<FormatException>(() => RunGetAllKeysCore(xml, new Mock<IActivator>().Object));
        }

        private static IReadOnlyCollection<IKey> RunGetAllKeysCore(string xml, IActivator activator, ILoggerFactory loggerFactory = null)
        {
            // Arrange
            var mockXmlRepository = new Mock<IXmlRepository>();
            mockXmlRepository.Setup(o => o.GetAllElements()).Returns(XElement.Parse(xml).Elements().ToArray());
            var options = Options.Create(new KeyManagementOptions()
            {
                AuthenticatedEncryptorConfiguration = new Mock<AlgorithmConfiguration>().Object,
                XmlRepository = mockXmlRepository.Object,
                XmlEncryptor = null
            });
            var keyManager = new XmlKeyManager(options, activator, loggerFactory ?? NullLoggerFactory.Instance);

            // Act
            return keyManager.GetAllKeys();
        }

        [Fact]
        public void RevokeAllKeys()
        {
            // Arrange
            XElement elementStoredInRepository = null;
            string friendlyNameStoredInRepository = null;
            var mockXmlRepository = new Mock<IXmlRepository>();
            mockXmlRepository
                .Setup(o => o.StoreElement(It.IsAny<XElement>(), It.IsAny<string>()))
                .Callback<XElement, string>((el, friendlyName) =>
                {
                    elementStoredInRepository = el;
                    friendlyNameStoredInRepository = friendlyName;
                });

            var options = Options.Create(new KeyManagementOptions()
            {
                AuthenticatedEncryptorConfiguration = new Mock<AlgorithmConfiguration>().Object,
                XmlRepository = mockXmlRepository.Object,
                XmlEncryptor = null
            });
            var keyManager = new XmlKeyManager(options, SimpleActivator.DefaultWithoutServices, NullLoggerFactory.Instance);

            var revocationDate = XmlConvert.ToDateTimeOffset("2015-03-01T19:13:19.7573854-08:00");

            // Act & assert

            // The cancellation token should not already be fired
            var firstCancellationToken = keyManager.GetCacheExpirationToken();
            Assert.False(firstCancellationToken.IsCancellationRequested);

            // After the call to RevokeAllKeys, the first CT should be fired,
            // and we should've gotten a new CT.
            keyManager.RevokeAllKeys(revocationDate, "Here's some reason text.");
            var secondCancellationToken = keyManager.GetCacheExpirationToken();
            Assert.True(firstCancellationToken.IsCancellationRequested);
            Assert.False(secondCancellationToken.IsCancellationRequested);

            // Was the correct element stored in the repository?
            const string expectedRepositoryXml = @"
                <revocation version='1'>
                  <revocationDate>2015-03-01T19:13:19.7573854-08:00</revocationDate>
                  <!--All keys created before the revocation date are revoked.-->
                  <key id='*' />
                  <reason>Here's some reason text.</reason>
                </revocation>";
            XmlAssert.Equal(expectedRepositoryXml, elementStoredInRepository);
            Assert.Equal("revocation-20150302T0313197573854Z", friendlyNameStoredInRepository);
        }

        [Fact]
        public void RevokeSingleKey_Internal()
        {
            // Arrange - mocks
            XElement elementStoredInRepository = null;
            string friendlyNameStoredInRepository = null;
            var mockXmlRepository = new Mock<IXmlRepository>();
            mockXmlRepository
                .Setup(o => o.StoreElement(It.IsAny<XElement>(), It.IsAny<string>()))
                .Callback<XElement, string>((el, friendlyName) =>
                {
                    elementStoredInRepository = el;
                    friendlyNameStoredInRepository = friendlyName;
                });

            var options = Options.Create(new KeyManagementOptions()
            {
                AuthenticatedEncryptorConfiguration = new Mock<AlgorithmConfiguration>().Object,
                XmlRepository = mockXmlRepository.Object,
                XmlEncryptor = null
            });
            var keyManager = new XmlKeyManager(options, SimpleActivator.DefaultWithoutServices, NullLoggerFactory.Instance);

            var revocationDate = new DateTimeOffset(2014, 01, 01, 0, 0, 0, TimeSpan.Zero);

            // Act & assert

            // The cancellation token should not already be fired
            var firstCancellationToken = keyManager.GetCacheExpirationToken();
            Assert.False(firstCancellationToken.IsCancellationRequested);

            // After the call to RevokeKey, the first CT should be fired,
            // and we should've gotten a new CT.
            ((IInternalXmlKeyManager)keyManager).RevokeSingleKey(
                keyId: new Guid("a11f35fc-1fed-4bd4-b727-056a63b70932"),
                revocationDate: revocationDate,
                reason: "Here's some reason text.");
            var secondCancellationToken = keyManager.GetCacheExpirationToken();
            Assert.True(firstCancellationToken.IsCancellationRequested);
            Assert.False(secondCancellationToken.IsCancellationRequested);

            // Was the correct element stored in the repository?
            var expectedRepositoryXml = string.Format(@"
                <revocation version='1'>
                  {0}
                  <key id='a11f35fc-1fed-4bd4-b727-056a63b70932' />
                  <reason>Here's some reason text.</reason>
                </revocation>",
                new XElement("revocationDate", revocationDate));
            XmlAssert.Equal(expectedRepositoryXml, elementStoredInRepository);
            Assert.Equal("revocation-a11f35fc-1fed-4bd4-b727-056a63b70932", friendlyNameStoredInRepository);
        }

        [Fact]
        public void RevokeKey_CallsInternalManager()
        {
            // Arrange
            var keyToRevoke = new Guid("a11f35fc-1fed-4bd4-b727-056a63b70932");
            DateTimeOffset minRevocationDate = DateTimeOffset.UtcNow;
            DateTimeOffset? actualRevocationDate = null;
            var mockInternalKeyManager = new Mock<IInternalXmlKeyManager>();
            mockInternalKeyManager
                .Setup(o => o.RevokeSingleKey(keyToRevoke, It.IsAny<DateTimeOffset>(), "Here's some reason text."))
                .Callback<Guid, DateTimeOffset, string>((innerKeyId, innerRevocationDate, innerReason) =>
                {
                    actualRevocationDate = innerRevocationDate;
                });

            var options = Options.Create(new KeyManagementOptions()
            {
                AuthenticatedEncryptorConfiguration = new Mock<AlgorithmConfiguration>().Object,
                XmlRepository = new Mock<IXmlRepository>().Object,
                XmlEncryptor = null
            });
            var keyManager = new XmlKeyManager(options, SimpleActivator.DefaultWithoutServices, NullLoggerFactory.Instance, mockInternalKeyManager.Object);

            // Act
            keyManager.RevokeKey(keyToRevoke, "Here's some reason text.");

            // Assert
            Assert.InRange(actualRevocationDate.Value, minRevocationDate, DateTimeOffset.UtcNow);
        }

        private class MyDeserializer : IAuthenticatedEncryptorDescriptorDeserializer
        {
            public IAuthenticatedEncryptorDescriptor ImportFromXml(XElement element)
            {
                throw new NotImplementedException();
            }
        }

        private class TestEncryptorFactory : IAuthenticatedEncryptorFactory
        {
            private IAuthenticatedEncryptorDescriptor _associatedDescriptor;
            private IAuthenticatedEncryptor _expectedEncryptor;

            public TestEncryptorFactory(IAuthenticatedEncryptorDescriptor associatedDescriptor = null, IAuthenticatedEncryptor expectedEncryptor = null)
            {
                _associatedDescriptor = associatedDescriptor;
                _expectedEncryptor = expectedEncryptor;
            }

            public IAuthenticatedEncryptor CreateEncryptorInstance(IKey key)
            {
                if (_associatedDescriptor != null && _associatedDescriptor != key.Descriptor)
                {
                    return null;
                }

                return _expectedEncryptor ?? new Mock<IAuthenticatedEncryptor>().Object;
            }
        }
    }
}
