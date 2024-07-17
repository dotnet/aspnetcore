// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
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

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

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
    public void Ctor_WithEncryptorButNoRepository_UsesFallback()
    {
        // Arrange
        var expectedXmlEncryptor = new Mock<IXmlEncryptor>().Object;
        var options = Options.Create(new KeyManagementOptions()
        {
            AuthenticatedEncryptorConfiguration = new Mock<AlgorithmConfiguration>().Object,
            XmlRepository = null,
            XmlEncryptor = expectedXmlEncryptor
        });

        // Act
        var keyManager = new XmlKeyManager(options, SimpleActivator.DefaultWithoutServices, NullLoggerFactory.Instance);

        // Assert
        Assert.NotNull(keyManager.KeyRepository);

        Assert.Same(expectedXmlEncryptor, keyManager.KeyEncryptor);
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
        string expectedXml = string.Format(
            CultureInfo.InvariantCulture,
            @"
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
        string expectedEscrowXml = string.Format(
          CultureInfo.InvariantCulture,
            @"
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
        string expectedRepositoryXml = String.Format(
          CultureInfo.InvariantCulture,
          @"
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
        var expectedRepositoryXml = string.Format(
          CultureInfo.InvariantCulture,
          @"
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

    [Fact]
    public void CreatedKeyReused()
    {
        // Arrange

        // The only descriptor we'll use
        var descriptor = new Mock<IAuthenticatedEncryptorDescriptor>(MockBehavior.Strict);
        descriptor
            .Setup(o => o.ExportToXml())
            // Shouldn't be an interface, but we control the activator and will return a mock
            .Returns(new XmlSerializedDescriptorInfo(serializedDescriptor, typeof(IAuthenticatedEncryptorDescriptorDeserializer)));

        // The factory always returns the only descriptor
        var authenticatedEncryptorConfiguration = new Mock<AlgorithmConfiguration>(MockBehavior.Strict);
        authenticatedEncryptorConfiguration
            .Setup(o => o.CreateNewDescriptor())
            .Returns(descriptor.Object);

        // Any xml element deserializes to the only descriptor
        var descriptorDeserializer = new Mock<IAuthenticatedEncryptorDescriptorDeserializer>(MockBehavior.Strict);
        descriptorDeserializer
            .Setup(o => o.ImportFromXml(It.IsAny<XElement>()))
            .Returns(descriptor.Object);

        // Keep track of how many times a key needs to be decrypted
        var decryptionCount = 0;
        var decryptor = new Mock<IXmlDecryptor>(MockBehavior.Strict);
        decryptor
            .Setup(o => o.Decrypt(It.IsAny<XElement>()))
            .Returns<XElement>(element =>
            {
                decryptionCount++;
                return element;
            });

        // We need a simple encryptor for the newly created key
        var encryptor = new Mock<IXmlEncryptor>(MockBehavior.Strict);
        encryptor
            .Setup(o => o.Encrypt(It.IsAny<XElement>()))
            // Shouldn't be an interface, but we control the activator and will return a mock
            .Returns<XElement>(element => new EncryptedXmlInfo(element, typeof(IXmlDecryptor)));

        // We control deserialization by hooking activation
        var activator = new Mock<IActivator>(MockBehavior.Strict);
        activator
            .Setup(o => o.CreateInstance(typeof(IXmlDecryptor), It.IsAny<string>()))
            .Returns(decryptor.Object);
        activator
            .Setup(o => o.CreateInstance(typeof(IAuthenticatedEncryptorDescriptorDeserializer), It.IsAny<string>()))
            .Returns(descriptorDeserializer.Object);

        var keyManagementOptions = Options.Create(new KeyManagementOptions()
        {
            AuthenticatedEncryptorConfiguration = authenticatedEncryptorConfiguration.Object,
            XmlRepository = new EphemeralXmlRepository(NullLoggerFactory.Instance), // A realistic repository is fine
            XmlEncryptor = encryptor.Object,
        });

        var keyManager = new XmlKeyManager(keyManagementOptions, activator.Object);

        var creationDate = DateTimeOffset.UtcNow;
        var activationDate = creationDate.AddDays(2);
        var expirationDate = creationDate.AddDays(90);

        var createdKey = keyManager.CreateNewKey(activationDate, expirationDate);

        // Force decryption, if encrypted.  The real call would be CreateEncryptor(), but that would access Descriptor under the hood
        _ = createdKey.Descriptor;
        Assert.Equal(0, decryptionCount);

        // Act
        var fetchedKeys = keyManager.GetAllKeys();

        // Assert
        var fetchedKey = Assert.Single(fetchedKeys);
        Assert.NotSame(createdKey, fetchedKey); // It's mutable, so we don't want to reuse the same instance
        Assert.Equal(createdKey.KeyId, fetchedKey.KeyId); // But it should be equivalent

        // Nothing up to this point should have required decryption
        Assert.Equal(0, decryptionCount);

        // Force decryption, if encrypted.  The real call would be CreateEncryptor(), but that would access Descriptor under the hood
        _ = fetchedKey.Descriptor;
        Assert.Equal(0, decryptionCount);

        var fetchedKeys2 = keyManager.GetAllKeys();

        var fetchedKey2 = Assert.Single(fetchedKeys2);
        Assert.NotSame(createdKey, fetchedKey2); // It's mutable, so we don't want to reuse the same instance
        Assert.NotSame(fetchedKey, fetchedKey2);
        Assert.Equal(createdKey.KeyId, fetchedKey2.KeyId); // But it should be equivalent

        // Force decryption, if encrypted.  The real call would be CreateEncryptor(), but that would access Descriptor under the hood
        _ = fetchedKey2.Descriptor;
        Assert.Equal(0, decryptionCount);
    }

    [Fact]
    public void NovelFetchedKeyRequiresDecryption()
    {
        // Arrange
        var descriptorDeserializer = new Mock<IAuthenticatedEncryptorDescriptorDeserializer>(MockBehavior.Strict);
        descriptorDeserializer
            .Setup(o => o.ImportFromXml(It.IsAny<XElement>()))
            .Returns(new Mock<IAuthenticatedEncryptorDescriptor>(MockBehavior.Strict).Object);

        // Keep track of how many times a key needs to be decrypted
        var decryptionCount = 0;
        var decryptor = new Mock<IXmlDecryptor>(MockBehavior.Strict);
        decryptor
            .Setup(o => o.Decrypt(It.IsAny<XElement>()))
            .Returns<XElement>(element =>
            {
                decryptionCount++;
                return element;
            });

        var encryptor = new Mock<IXmlEncryptor>(MockBehavior.Strict);

        // We control deserialization by hooking activation
        var activator = new Mock<IActivator>(MockBehavior.Strict);
        activator
            .Setup(o => o.CreateInstance(typeof(IXmlDecryptor), It.IsAny<string>()))
            .Returns(decryptor.Object);
        activator
            .Setup(o => o.CreateInstance(typeof(IAuthenticatedEncryptorDescriptorDeserializer), It.IsAny<string>()))
            .Returns(descriptorDeserializer.Object);

        var keyElement = XElement.Parse(@"
<key id=""3d6d01fd-c0e7-44ae-82dd-013b996b4093"">
    <creationDate>2015-01-01T00:00:00Z</creationDate>
    <activationDate>2015-01-02T00:00:00Z</activationDate>
    <expirationDate>2015-03-01T00:00:00Z</expirationDate>
    <descriptor deserializerType=""SomeAQN1"">
        <descriptor>
        <encryption algorithm=""AES_256_CBC"" />
        <validation algorithm=""HMACSHA256"" />
        <encryptedSecret decryptorType=""SomeAQN2"" xmlns=""http://schemas.asp.net/2015/03/dataProtection"">
            <encryptedKey xmlns="""">
                <value>KeyAsBase64</value>
            </encryptedKey>
        </encryptedSecret>
        </descriptor>
    </descriptor>
</key>
");

        var respository = new Mock<IXmlRepository>();
        respository
            .Setup(o => o.GetAllElements())
            .Returns([keyElement]);

        var keyManagementOptions = Options.Create(new KeyManagementOptions()
        {
            XmlRepository = respository.Object,
            XmlEncryptor = encryptor.Object,
        });

        var keyManager = new XmlKeyManager(keyManagementOptions, activator.Object);

        // Act
        var fetchedKeys = keyManager.GetAllKeys();

        // Assert
        var fetchedKey = Assert.Single(fetchedKeys);

        // Decryption is lazy
        Assert.Equal(0, decryptionCount);

        // Force decryption.  The real call would be CreateEncryptor(), but that would access Descriptor under the hood
        _ = fetchedKey.Descriptor;
        Assert.Equal(1, decryptionCount);

        var fetchedKeys2 = keyManager.GetAllKeys();

        var fetchedKey2 = Assert.Single(fetchedKeys2);
        Assert.NotSame(fetchedKey, fetchedKey2); // It's mutable, so we don't want to reuse the same instance
        Assert.Equal(fetchedKey.KeyId, fetchedKey2.KeyId); // But it should be equivalent

        // Force decryption, if encrypted.  The real call would be CreateEncryptor(), but that would access Descriptor under the hood
        _ = fetchedKey2.Descriptor;
        Assert.Equal(1, decryptionCount); // Still 1 (i.e. no change)
    }

    [Fact]
    public void DeleteKeys()
    {
        var repository = new EphemeralXmlRepository(NullLoggerFactory.Instance);

        var keyManager = new XmlKeyManager(
            Options.Create(new KeyManagementOptions()
            {
                AuthenticatedEncryptorConfiguration = new AuthenticatedEncryptorConfiguration(),
                XmlRepository = repository,
                XmlEncryptor = null
            }),
            SimpleActivator.DefaultWithoutServices,
            NullLoggerFactory.Instance);

        var activationTime = DateTimeOffset.UtcNow.AddHours(1);

        var key1 = keyManager.CreateNewKey(activationTime, activationTime.AddMinutes(10));
        keyManager.RevokeAllKeys(DateTimeOffset.UtcNow, "Revoking all keys"); // This should revoke key1
        var key2 = keyManager.CreateNewKey(activationTime, activationTime.AddMinutes(10));
        keyManager.RevokeAllKeys(DateTimeOffset.UtcNow, "Revoking all keys"); // This should revoke key1 and key2
        var key3 = keyManager.CreateNewKey(activationTime, activationTime.AddMinutes(10));
        var key4 = keyManager.CreateNewKey(activationTime, activationTime.AddMinutes(10));

        keyManager.RevokeKey(key2.KeyId); // Revoked by time, but also individually
        keyManager.RevokeKey(key3.KeyId);
        keyManager.RevokeKey(key3.KeyId); // Nothing prevents us from revoking the same key twice

        Assert.Equal(9, repository.GetAllElements().Count); // 4 keys, 2 time-revocations, 3 guid-revocations

        // The keys are stale now, but we only care about the IDs

        var keyDictWithRevocations = keyManager.GetAllKeys().ToDictionary(k => k.KeyId);
        Assert.Equal(4, keyDictWithRevocations.Count);
        Assert.True(keyDictWithRevocations[key1.KeyId].IsRevoked);
        Assert.True(keyDictWithRevocations[key2.KeyId].IsRevoked);
        Assert.True(keyDictWithRevocations[key3.KeyId].IsRevoked);
        Assert.False(keyDictWithRevocations[key4.KeyId].IsRevoked);

        Assert.True(keyManager.DeleteKeys(key => key.KeyId == key1.KeyId || key.KeyId == key3.KeyId));

        Assert.Equal(4, repository.GetAllElements().Count); // 2 keys, 1 time-revocation, 1 guid-revocations

        var keyDictWithDeletions = keyManager.GetAllKeys().ToDictionary(k => k.KeyId);
        Assert.Equal(2, keyDictWithDeletions.Count);
        Assert.DoesNotContain(key1.KeyId, keyDictWithDeletions.Keys);
        Assert.True(keyDictWithRevocations[key2.KeyId].IsRevoked);
        Assert.DoesNotContain(key3.KeyId, keyDictWithDeletions.Keys);
        Assert.False(keyDictWithRevocations[key4.KeyId].IsRevoked);
    }

    [Fact]
    public void CanDeleteKey()
    {
        var withDeletion = new XmlKeyManager(Options.Create(new KeyManagementOptions()
        {
            XmlRepository = XmlRepositoryWithDeletion.Instance,
            XmlEncryptor = null
        }), SimpleActivator.DefaultWithoutServices, NullLoggerFactory.Instance);
        Assert.True(withDeletion.CanDeleteKeys);

        var withoutDeletion = new XmlKeyManager(Options.Create(new KeyManagementOptions()
        {
            XmlRepository = XmlRepositoryWithoutDeletion.Instance,
            XmlEncryptor = null
        }), SimpleActivator.DefaultWithoutServices, NullLoggerFactory.Instance);
        Assert.False(withoutDeletion.CanDeleteKeys);
        Assert.Throws<NotSupportedException>(() => withoutDeletion.DeleteKeys(_ => false));
    }

    private sealed class XmlRepositoryWithoutDeletion : IXmlRepository
    {
        public static readonly IXmlRepository Instance = new XmlRepositoryWithoutDeletion();

        private XmlRepositoryWithoutDeletion() { }

        IReadOnlyCollection<XElement> IXmlRepository.GetAllElements() => [];
        void IXmlRepository.StoreElement(XElement element, string friendlyName) => throw new InvalidOperationException();
    }

    private sealed class XmlRepositoryWithDeletion : IDeletableXmlRepository
    {
        public static readonly IDeletableXmlRepository Instance = new XmlRepositoryWithDeletion();

        private XmlRepositoryWithDeletion() { }

        IReadOnlyCollection<XElement> IXmlRepository.GetAllElements() => [];
        void IXmlRepository.StoreElement(XElement element, string friendlyName) => throw new InvalidOperationException();
        bool IDeletableXmlRepository.DeleteElements(Action<IReadOnlyCollection<IDeletableElement>> chooseElements) => throw new InvalidOperationException();
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
