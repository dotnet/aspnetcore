// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.Internal;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            var expectedEncryptor = new Mock<IXmlEncryptor>().Object;
            var expectedRepository = new Mock<IXmlRepository>().Object;
            var mockFallback = new Mock<IDefaultKeyServices>();
            mockFallback.Setup(o => o.GetKeyEncryptor()).Returns(expectedEncryptor);
            mockFallback.Setup(o => o.GetKeyRepository()).Returns(expectedRepository);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IDefaultKeyServices>(mockFallback.Object);
            serviceCollection.AddSingleton<IAuthenticatedEncryptorConfiguration>(new Mock<IAuthenticatedEncryptorConfiguration>().Object);
            var services = serviceCollection.BuildServiceProvider();

            // Act
            var keyManager = new XmlKeyManager(services);

            // Assert
            Assert.Same(expectedEncryptor, keyManager.KeyEncryptor);
            Assert.Same(expectedRepository, keyManager.KeyRepository);
        }

        [Fact]
        public void Ctor_WithEncryptorButNoRepository_IgnoresFallback_FailsWithServiceNotFound()
        {
            // Arrange
            var mockFallback = new Mock<IDefaultKeyServices>();
            mockFallback.Setup(o => o.GetKeyEncryptor()).Returns(new Mock<IXmlEncryptor>().Object);
            mockFallback.Setup(o => o.GetKeyRepository()).Returns(new Mock<IXmlRepository>().Object);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IDefaultKeyServices>(mockFallback.Object);
            serviceCollection.AddSingleton<IXmlEncryptor>(new Mock<IXmlEncryptor>().Object);
            serviceCollection.AddSingleton<IAuthenticatedEncryptorConfiguration>(new Mock<IAuthenticatedEncryptorConfiguration>().Object);
            var services = serviceCollection.BuildServiceProvider();

            // Act & assert - we don't care about exception type, only exception message
            Exception ex = Assert.ThrowsAny<Exception>(() => new XmlKeyManager(services));
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

            // Arrange - mocks
            XElement elementStoredInRepository = null;
            string friendlyNameStoredInRepository = null;
            var expectedAuthenticatedEncryptor = new Mock<IAuthenticatedEncryptor>().Object;
            var mockDescriptor = new Mock<IAuthenticatedEncryptorDescriptor>();
            mockDescriptor.Setup(o => o.ExportToXml()).Returns(new XmlSerializedDescriptorInfo(serializedDescriptor, typeof(MyDeserializer)));
            mockDescriptor.Setup(o => o.CreateEncryptorInstance()).Returns(expectedAuthenticatedEncryptor);
            var mockConfiguration = new Mock<IAuthenticatedEncryptorConfiguration>();
            mockConfiguration.Setup(o => o.CreateNewDescriptor()).Returns(mockDescriptor.Object);
            var mockXmlRepository = new Mock<IXmlRepository>();
            mockXmlRepository
                .Setup(o => o.StoreElement(It.IsAny<XElement>(), It.IsAny<string>()))
                .Callback<XElement, string>((el, friendlyName) =>
                {
                    elementStoredInRepository = el;
                    friendlyNameStoredInRepository = friendlyName;
                });

            // Arrange - services
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IXmlRepository>(mockXmlRepository.Object);
            serviceCollection.AddSingleton<IAuthenticatedEncryptorConfiguration>(mockConfiguration.Object);
            var services = serviceCollection.BuildServiceProvider();
            var keyManager = new XmlKeyManager(services);

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
            Assert.False(newKey.IsRevoked);
            Assert.Same(expectedAuthenticatedEncryptor, newKey.CreateEncryptorInstance());

            // Finally, was the correct element stored in the repository?
            string expectedXml = String.Format(@"
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

            // Arrange - mocks
            XElement elementStoredInEscrow = null;
            Guid? keyIdStoredInEscrow = null;
            XElement elementStoredInRepository = null;
            string friendlyNameStoredInRepository = null;
            var expectedAuthenticatedEncryptor = new Mock<IAuthenticatedEncryptor>().Object;
            var mockDescriptor = new Mock<IAuthenticatedEncryptorDescriptor>();
            mockDescriptor.Setup(o => o.ExportToXml()).Returns(new XmlSerializedDescriptorInfo(serializedDescriptor, typeof(MyDeserializer)));
            mockDescriptor.Setup(o => o.CreateEncryptorInstance()).Returns(expectedAuthenticatedEncryptor);
            var mockConfiguration = new Mock<IAuthenticatedEncryptorConfiguration>();
            mockConfiguration.Setup(o => o.CreateNewDescriptor()).Returns(mockDescriptor.Object);
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

            // Arrange - services
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IXmlRepository>(mockXmlRepository.Object);
            serviceCollection.AddSingleton<IAuthenticatedEncryptorConfiguration>(mockConfiguration.Object);
            serviceCollection.AddSingleton<IKeyEscrowSink>(mockKeyEscrow.Object);
            serviceCollection.AddSingleton<IXmlEncryptor, NullXmlEncryptor>();
            var services = serviceCollection.BuildServiceProvider();
            var keyManager = new XmlKeyManager(services);

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
            Assert.False(newKey.IsRevoked);
            Assert.Same(expectedAuthenticatedEncryptor, newKey.CreateEncryptorInstance());

            // Was the correct element stored in escrow?
            // This should not have gone through the encryptor.
            string expectedEscrowXml = String.Format(@"
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
            // Arrange - mocks
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

            // Arrange - services
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IXmlRepository>(new Mock<IXmlRepository>().Object);
            serviceCollection.AddSingleton<IAuthenticatedEncryptorConfiguration>(new Mock<IAuthenticatedEncryptorConfiguration>().Object);
            serviceCollection.AddSingleton<IInternalXmlKeyManager>(mockInternalKeyManager.Object);
            var services = serviceCollection.BuildServiceProvider();
            var keyManager = new XmlKeyManager(services);

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

            var encryptorA = new Mock<IAuthenticatedEncryptor>().Object;
            var encryptorB = new Mock<IAuthenticatedEncryptor>().Object;
            var mockActivator = new Mock<IActivator>();
            mockActivator.ReturnAuthenticatedEncryptorGivenDeserializerTypeNameAndInput("deserializer-A", "<elementA />", encryptorA);
            mockActivator.ReturnAuthenticatedEncryptorGivenDeserializerTypeNameAndInput("deserializer-B", "<elementB />", encryptorB);

            // Act
            var keys = RunGetAllKeysCore(xml, mockActivator.Object).ToArray();

            // Assert
            Assert.Equal(2, keys.Length);
            Assert.Equal(new Guid("62a72ad9-42d7-4e97-b3fa-05bad5d53d33"), keys[0].KeyId);
            Assert.Equal(XmlConvert.ToDateTimeOffset("2015-01-01T00:00:00Z"), keys[0].CreationDate);
            Assert.Equal(XmlConvert.ToDateTimeOffset("2015-02-01T00:00:00Z"), keys[0].ActivationDate);
            Assert.Equal(XmlConvert.ToDateTimeOffset("2015-03-01T00:00:00Z"), keys[0].ExpirationDate);
            Assert.False(keys[0].IsRevoked);
            Assert.Same(encryptorA, keys[0].CreateEncryptorInstance());
            Assert.Equal(new Guid("041be4c0-52d7-48b4-8d32-f8c0ff315459"), keys[1].KeyId);
            Assert.Equal(XmlConvert.ToDateTimeOffset("2015-04-01T00:00:00Z"), keys[1].CreationDate);
            Assert.Equal(XmlConvert.ToDateTimeOffset("2015-05-01T00:00:00Z"), keys[1].ActivationDate);
            Assert.Equal(XmlConvert.ToDateTimeOffset("2015-06-01T00:00:00Z"), keys[1].ExpirationDate);
            Assert.False(keys[1].IsRevoked);
            Assert.Same(encryptorB, keys[1].CreateEncryptorInstance());
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
            mockActivator.ReturnAuthenticatedEncryptorGivenDeserializerTypeNameAndInput("theDeserializer", "<node />", new Mock<IAuthenticatedEncryptor>().Object);

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

            var expectedEncryptor = new Mock<IAuthenticatedEncryptor>().Object;
            var mockActivator = new Mock<IActivator>();
            mockActivator.ReturnDecryptedElementGivenDecryptorTypeNameAndInput("theDecryptor", "<node xmlns='private' />", "<decryptedNode />");
            mockActivator.ReturnAuthenticatedEncryptorGivenDeserializerTypeNameAndInput("theDeserializer", "<decryptedNode />", expectedEncryptor);

            // Act
            var keys = RunGetAllKeysCore(xml, mockActivator.Object).ToArray();

            // Assert
            Assert.Equal(1, keys.Length);
            Assert.Equal(new Guid("09712588-ba68-438a-a5ee-fe842b3453b2"), keys[0].KeyId);
            Assert.Same(expectedEncryptor, keys[0].CreateEncryptorInstance());
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

            var expectedEncryptor = new Mock<IAuthenticatedEncryptor>().Object;
            var mockActivator = new Mock<IActivator>();
            mockActivator.ReturnAuthenticatedEncryptorGivenDeserializerTypeNameAndInput("goodDeserializer", "<node xmlns='private' />", expectedEncryptor);

            // Act
            var keys = RunGetAllKeysCore(xml, mockActivator.Object).ToArray();

            // Assert
            Assert.Equal(1, keys.Length);
            Assert.Equal(new Guid("49c0cda9-0232-4d8c-a541-de20cc5a73d6"), keys[0].KeyId);
            Assert.Same(expectedEncryptor, keys[0].CreateEncryptorInstance());
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
            // Arrange - mocks
            var mockXmlRepository = new Mock<IXmlRepository>();
            mockXmlRepository.Setup(o => o.GetAllElements()).Returns(XElement.Parse(xml).Elements().ToArray());

            // Arrange - services
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IXmlRepository>(mockXmlRepository.Object);
            serviceCollection.AddSingleton<IActivator>(activator);
            serviceCollection.AddSingleton<IAuthenticatedEncryptorConfiguration>(new Mock<IAuthenticatedEncryptorConfiguration>().Object);
            if (loggerFactory != null)
            {
                serviceCollection.AddSingleton<ILoggerFactory>(loggerFactory);
            }
            var services = serviceCollection.BuildServiceProvider();
            var keyManager = new XmlKeyManager(services);

            // Act
            return keyManager.GetAllKeys();
        }

        [Fact]
        public void RevokeAllKeys()
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

            // Arrange - services
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IXmlRepository>(mockXmlRepository.Object);
            serviceCollection.AddSingleton<IAuthenticatedEncryptorConfiguration>(new Mock<IAuthenticatedEncryptorConfiguration>().Object);
            var services = serviceCollection.BuildServiceProvider();
            var keyManager = new XmlKeyManager(services);

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

            // Arrange - services
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IXmlRepository>(mockXmlRepository.Object);
            serviceCollection.AddSingleton<IAuthenticatedEncryptorConfiguration>(new Mock<IAuthenticatedEncryptorConfiguration>().Object);
            var services = serviceCollection.BuildServiceProvider();
            var keyManager = new XmlKeyManager(services);

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
            // Arrange - mocks
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

            // Arrange - services
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IXmlRepository>(new Mock<IXmlRepository>().Object);
            serviceCollection.AddSingleton<IAuthenticatedEncryptorConfiguration>(new Mock<IAuthenticatedEncryptorConfiguration>().Object);
            serviceCollection.AddSingleton<IInternalXmlKeyManager>(mockInternalKeyManager.Object);
            var services = serviceCollection.BuildServiceProvider();
            var keyManager = new XmlKeyManager(services);

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
    }
}
