// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Internal;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption;

public class XmlEncryptionExtensionsTests
{
    [Fact]
    public void DecryptElement_NothingToDecrypt_ReturnsOriginalElement()
    {
        // Arrange
        var original = XElement.Parse(@"<element />");

        // Act
        var retVal = original.DecryptElement(activator: null);

        // Assert
        Assert.Same(original, retVal);
        XmlAssert.Equal("<element />", original); // unmutated
    }

    [Fact]
    public void DecryptElement_RootNodeRequiresDecryption_Success()
    {
        // Arrange
        var original = XElement.Parse(@"
                <x:encryptedSecret decryptorType='theDecryptor' xmlns:x='http://schemas.asp.net/2015/03/dataProtection'>
                  <node />
                </x:encryptedSecret>");

        var mockActivator = new Mock<IActivator>();
        mockActivator.ReturnDecryptedElementGivenDecryptorTypeNameAndInput("theDecryptor", "<node />", "<newNode />");

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IActivator>(mockActivator.Object);
        var services = serviceCollection.BuildServiceProvider();
        var activator = services.GetActivator();

        // Act
        var retVal = original.DecryptElement(activator);

        // Assert
        XmlAssert.Equal("<newNode />", retVal);
    }

    [Fact]
    public void DecryptElement_CustomType_TypeNameResolverNotCalled()
    {
        // Arrange
        var decryptorTypeName = typeof(MyXmlDecryptor).AssemblyQualifiedName;

        var original = XElement.Parse(@$"
                <x:encryptedSecret decryptorType='{decryptorTypeName}' xmlns:x='http://schemas.asp.net/2015/03/dataProtection'>
                  <node />
                </x:encryptedSecret>");

        var mockActivator = new Mock<IActivator>();
        mockActivator.ReturnDecryptedElementGivenDecryptorTypeNameAndInput(decryptorTypeName, "<node />", "<newNode />");
        var mockTypeNameResolver = mockActivator.As<ITypeNameResolver>();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IActivator>(mockActivator.Object);
        var services = serviceCollection.BuildServiceProvider();
        var activator = services.GetActivator();

        // Act
        var retVal = original.DecryptElement(activator);

        // Assert
        XmlAssert.Equal("<newNode />", retVal);
        Type resolvedType;
        mockTypeNameResolver.Verify(o => o.TryResolveType(It.IsAny<string>(), out resolvedType), Times.Never());
    }

    [Fact]
    public void DecryptElement_KnownType_TypeNameResolverCalled()
    {
        // Arrange
        var decryptorTypeName = typeof(NullXmlDecryptor).AssemblyQualifiedName;
        TypeForwardingActivator.TryForwardTypeName(decryptorTypeName, out var forwardedTypeName);

        var original = XElement.Parse(@$"
                <x:encryptedSecret decryptorType='{decryptorTypeName}' xmlns:x='http://schemas.asp.net/2015/03/dataProtection'>
                  <node>
                    <value />
                  </node>
                </x:encryptedSecret>");

        var mockActivator = new Mock<IActivator>();
        mockActivator.Setup(o => o.CreateInstance(typeof(NullXmlDecryptor), decryptorTypeName)).Returns(new NullXmlDecryptor());
        var mockTypeNameResolver = mockActivator.As<ITypeNameResolver>();
        var resolvedType = typeof(NullXmlDecryptor);
        mockTypeNameResolver.Setup(mockTypeNameResolver => mockTypeNameResolver.TryResolveType(forwardedTypeName, out resolvedType)).Returns(true);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IActivator>(mockActivator.Object);
        var services = serviceCollection.BuildServiceProvider();
        var activator = services.GetActivator();

        // Act
        var retVal = original.DecryptElement(activator);

        // Assert
        XmlAssert.Equal("<value />", retVal);
        mockTypeNameResolver.Verify(o => o.TryResolveType(It.IsAny<string>(), out resolvedType), Times.Once());
    }

    [Fact]
    public void DecryptElement_KnownType_UnableToResolveType_Success()
    {
        // Arrange
        var decryptorTypeName = typeof(NullXmlDecryptor).AssemblyQualifiedName;

        var original = XElement.Parse(@$"
                <x:encryptedSecret decryptorType='{decryptorTypeName}' xmlns:x='http://schemas.asp.net/2015/03/dataProtection'>
                  <node>
                    <value />
                  </node>
                </x:encryptedSecret>");

        var mockActivator = new Mock<IActivator>();
        mockActivator.Setup(o => o.CreateInstance(typeof(IXmlDecryptor), decryptorTypeName)).Returns(new NullXmlDecryptor());
        var mockTypeNameResolver = mockActivator.As<ITypeNameResolver>();
        Type resolvedType = null;
        mockTypeNameResolver.Setup(mockTypeNameResolver => mockTypeNameResolver.TryResolveType(It.IsAny<string>(), out resolvedType)).Returns(false);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IActivator>(mockActivator.Object);
        var services = serviceCollection.BuildServiceProvider();
        var activator = services.GetActivator();

        // Act
        var retVal = original.DecryptElement(activator);

        // Assert
        XmlAssert.Equal("<value />", retVal);
        mockTypeNameResolver.Verify(o => o.TryResolveType(It.IsAny<string>(), out resolvedType), Times.Once());
    }

    [Fact]
    public void DecryptElement_MultipleNodesRequireDecryption_AvoidsRecursion_Success()
    {
        // Arrange
        var original = XElement.Parse(@"
                <rootNode xmlns:x='http://schemas.asp.net/2015/03/dataProtection'>
                  <x:encryptedSecret decryptorType='myDecryptor'>
                    <node1 />
                  </x:encryptedSecret>
                  <node2 x:requiresEncryption='false'>
                    <![CDATA[This data should stick around.]]>
                    <x:encryptedSecret decryptorType='myDecryptor'>
                      <node3 />
                    </x:encryptedSecret>
                  </node2>
                </rootNode>");

        var expected = @"
                <rootNode xmlns:x='http://schemas.asp.net/2015/03/dataProtection'>
                  <node1_decrypted>
                    <x:encryptedSecret>nested</x:encryptedSecret>
                  </node1_decrypted>
                  <node2 x:requiresEncryption='false'>
                    <![CDATA[This data should stick around.]]>
                      <node3_decrypted>
                        <x:encryptedSecret>nested</x:encryptedSecret>
                      </node3_decrypted>
                  </node2>
                </rootNode>";

        var mockDecryptor = new Mock<IXmlDecryptor>();
        mockDecryptor
            .Setup(o => o.Decrypt(It.IsAny<XElement>()))
            .Returns<XElement>(el => new XElement(el.Name.LocalName + "_decrypted", new XElement(XmlConstants.EncryptedSecretElementName, "nested")));

        var mockActivator = new Mock<IActivator>();
        mockActivator.Setup(o => o.CreateInstance(typeof(IXmlDecryptor), "myDecryptor")).Returns(mockDecryptor.Object);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IActivator>(mockActivator.Object);
        var services = serviceCollection.BuildServiceProvider();
        var activator = services.GetActivator();

        // Act
        var retVal = original.DecryptElement(activator);

        // Assert
        XmlAssert.Equal(expected, retVal);
    }

    [Fact]
    public void EncryptIfNecessary_NothingToEncrypt_ReturnsNull()
    {
        // Arrange
        var original = XElement.Parse(@"<element />");
        var xmlEncryptor = new Mock<IXmlEncryptor>(MockBehavior.Strict).Object;

        // Act
        var retVal = xmlEncryptor.EncryptIfNecessary(original);

        // Assert
        Assert.Null(retVal);
        XmlAssert.Equal("<element />", original); // unmutated
    }

    [Fact]
    public void EncryptIfNecessary_RootNodeRequiresEncryption_Success()
    {
        // Arrange
        var original = XElement.Parse(@"<rootNode x:requiresEncryption='true' xmlns:x='http://schemas.asp.net/2015/03/dataProtection' />");
        var mockXmlEncryptor = new Mock<IXmlEncryptor>();
        mockXmlEncryptor.Setup(o => o.Encrypt(It.IsAny<XElement>())).Returns(new EncryptedXmlInfo(new XElement("theElement"), typeof(MyXmlDecryptor)));

        // Act
        var retVal = mockXmlEncryptor.Object.EncryptIfNecessary(original);

        // Assert
        XmlAssert.Equal(@"<rootNode x:requiresEncryption='true' xmlns:x='http://schemas.asp.net/2015/03/dataProtection' />", original); // unmutated
        Assert.Equal(XmlConstants.EncryptedSecretElementName, retVal.Name);
        Assert.Equal(typeof(MyXmlDecryptor).AssemblyQualifiedName, (string)retVal.Attribute(XmlConstants.DecryptorTypeAttributeName));
        XmlAssert.Equal("<theElement />", retVal.Descendants().Single());
    }

    [Fact]
    public void EncryptIfNecessary_MultipleNodesRequireEncryption_Success()
    {
        // Arrange
        var original = XElement.Parse(@"
                <rootNode xmlns:x='http://schemas.asp.net/2015/03/dataProtection'>
                  <node1 x:requiresEncryption='true'>
                    <![CDATA[This data should be encrypted.]]>
                  </node1>
                  <node2 x:requiresEncryption='false'>
                    <![CDATA[This data should stick around.]]>
                    <node3 x:requiresEncryption='true'>
                      <node4 x:requiresEncryption='true' />
                    </node3>
                  </node2>
                </rootNode>");

        var expected = string.Format(
          CultureInfo.InvariantCulture,
          @"
                <rootNode xmlns:x='http://schemas.asp.net/2015/03/dataProtection'>
                  <x:encryptedSecret decryptorType='{0}'>
                    <node1_encrypted />
                  </x:encryptedSecret>
                  <node2 x:requiresEncryption='false'>
                    <![CDATA[This data should stick around.]]>
                    <x:encryptedSecret decryptorType='{0}'>
                      <node3_encrypted />
                    </x:encryptedSecret>
                  </node2>
                </rootNode>",
            typeof(MyXmlDecryptor).AssemblyQualifiedName);

        var mockXmlEncryptor = new Mock<IXmlEncryptor>();
        mockXmlEncryptor
            .Setup(o => o.Encrypt(It.IsAny<XElement>()))
            .Returns<XElement>(element => new EncryptedXmlInfo(new XElement(element.Name.LocalName + "_encrypted"), typeof(MyXmlDecryptor)));

        // Act
        var retVal = mockXmlEncryptor.Object.EncryptIfNecessary(original);

        // Assert
        XmlAssert.Equal(expected, retVal);
    }

    [Fact]
    public void EncryptIfNecessary_NullEncryptorWithRecursion_NoStackDive_Success()
    {
        // Arrange
        var original = XElement.Parse(@"
                <rootNode xmlns:x='http://schemas.asp.net/2015/03/dataProtection'>
                  <node1 x:requiresEncryption='true'>
                    <![CDATA[This data should be encrypted.]]>
                  </node1>
                  <node2 x:requiresEncryption='false'>
                    <![CDATA[This data should stick around.]]>
                    <node3 x:requiresEncryption='true'>
                      <node4 x:requiresEncryption='true' />
                    </node3>
                  </node2>
                </rootNode>");

        var expected = string.Format(
          CultureInfo.InvariantCulture,
          @"
                <rootNode xmlns:x='http://schemas.asp.net/2015/03/dataProtection'>
                  <x:encryptedSecret decryptorType='{0}'>
                    <node1 x:requiresEncryption='true'>
                      <![CDATA[This data should be encrypted.]]>
                    </node1>
                  </x:encryptedSecret>
                  <node2 x:requiresEncryption='false'>
                    <![CDATA[This data should stick around.]]>
                    <x:encryptedSecret decryptorType='{0}'>
                      <node3 x:requiresEncryption='true'>
                          <node4 x:requiresEncryption='true' />
                      </node3>
                    </x:encryptedSecret>
                  </node2>
                </rootNode>",
            typeof(MyXmlDecryptor).AssemblyQualifiedName);

        var mockXmlEncryptor = new Mock<IXmlEncryptor>();
        mockXmlEncryptor
            .Setup(o => o.Encrypt(It.IsAny<XElement>()))
            .Returns<XElement>(element => new EncryptedXmlInfo(new XElement(element), typeof(MyXmlDecryptor)));

        // Act
        var retVal = mockXmlEncryptor.Object.EncryptIfNecessary(original);

        // Assert
        XmlAssert.Equal(expected, retVal);
    }

    private sealed class MyXmlDecryptor : IXmlDecryptor
    {
        public XElement Decrypt(XElement encryptedElement)
        {
            throw new NotImplementedException();
        }
    }
}
