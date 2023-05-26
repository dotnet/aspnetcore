// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.Internal;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Moq;

namespace Microsoft.AspNetCore.DataProtection;

internal static class MockExtensions
{
    /// <summary>
    /// Sets up a mock such that given the name of a deserializer class and the XML node that class's
    /// Import method should expect returns a descriptor which produces the given authenticator.
    /// </summary>
    public static void ReturnDescriptorGivenDeserializerTypeNameAndInput(this Mock<IActivator> mockActivator, string typeName, string xml, IAuthenticatedEncryptorDescriptor descriptor)
    {
        mockActivator
            .Setup(o => o.CreateInstance(typeof(IAuthenticatedEncryptorDescriptorDeserializer), typeName))
            .Returns(() =>
            {
                var mockDeserializer = new Mock<IAuthenticatedEncryptorDescriptorDeserializer>();
                mockDeserializer
                    .Setup(o => o.ImportFromXml(It.IsAny<XElement>()))
                    .Returns<XElement>(el =>
                    {
                        // Only return the descriptor if the XML matches
                        XmlAssert.Equal(xml, el);
                        return descriptor;
                    });
                return mockDeserializer.Object;
            });
    }

    /// <summary>
    /// Sets up a mock such that given the name of a decryptor class and the XML node that class's
    /// Decrypt method should expect returns the specified XML elmeent.
    /// </summary>
    public static void ReturnDecryptedElementGivenDecryptorTypeNameAndInput(this Mock<IActivator> mockActivator, string typeName, string expectedInputXml, string outputXml)
    {
        mockActivator
            .Setup(o => o.CreateInstance(typeof(IXmlDecryptor), typeName))
            .Returns(() =>
            {
                var mockDecryptor = new Mock<IXmlDecryptor>();
                mockDecryptor
                    .Setup(o => o.Decrypt(It.IsAny<XElement>()))
                    .Returns<XElement>(el =>
                    {
                        // Only return the descriptor if the XML matches
                        XmlAssert.Equal(expectedInputXml, el);
                        return XElement.Parse(outputXml);
                    });
                return mockDecryptor.Object;
            });
    }
}
