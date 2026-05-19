// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.Internal;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption;

internal static unsafe class XmlEncryptionExtensions
{
    public static XElement DecryptElement(this XElement element, IActivator activator)
    {
        // If no decryption necessary, return original element.
        if (!DoesElementOrDescendentRequireDecryption(element))
        {
            return element;
        }

        // Deep copy the element (since we're going to mutate) and put
        // it into a document to guarantee it has a parent.
        var doc = new XDocument(new XElement(element));

        // We remove elements from the document as we decrypt them and perform
        // fix-up later. This keeps us from going into an infinite loop in
        // the case of a null decryptor (which returns its original input which
        // is still marked as 'requires decryption').
        var placeholderReplacements = new Dictionary<XElement, XElement>();

        while (true)
        {
            var elementWhichRequiresDecryption = doc.Descendants(XmlConstants.EncryptedSecretElementName).FirstOrDefault();
            if (elementWhichRequiresDecryption == null)
            {
                // All encryption is finished.
                break;
            }

            // Decrypt the clone so that the decryptor doesn't inadvertently modify
            // the original document or other data structures. The element we pass to
            // the decryptor should be the child of the 'encryptedSecret' element.
            var clonedElementWhichRequiresDecryption = new XElement(elementWhichRequiresDecryption);
            string decryptorTypeName = (string)clonedElementWhichRequiresDecryption.Attribute(XmlConstants.DecryptorTypeAttributeName)!;
            var decryptorInstance = CreateDecryptor(activator, decryptorTypeName);
            var decryptedElement = decryptorInstance.Decrypt(clonedElementWhichRequiresDecryption.Elements().Single());

            // Put a placeholder into the original document so that we can continue our
            // search for elements which need to be decrypted.
            var newPlaceholder = new XElement("placeholder");
            placeholderReplacements[newPlaceholder] = decryptedElement;
            elementWhichRequiresDecryption.ReplaceWith(newPlaceholder);
        }

        // Finally, perform fixup.
        Debug.Assert(placeholderReplacements.Count > 0);
        foreach (var entry in placeholderReplacements)
        {
            entry.Key.ReplaceWith(entry.Value);
        }
        return doc.Root!;
    }

    private static IXmlDecryptor CreateDecryptor(IActivator activator, string decryptorTypeName)
    {
        // typeNameToMatch will be used for matching against known types but not passed to the activator.
        // The activator will do its own forwarding.
        var typeNameToMatch = TypeForwardingActivator.TryForwardTypeName(decryptorTypeName, out var forwardedTypeName)
            ? forwardedTypeName
            : decryptorTypeName;

        // Note: ITypeNameResolver is only implemented on the activator in tests. In production, it's always DefaultTypeNameResolver.
        var typeNameResolver = activator as ITypeNameResolver ?? DefaultTypeNameResolver.Instance;

        if (typeof(DpapiNGXmlDecryptor).MatchName(typeNameToMatch, typeNameResolver))
        {
            return activator.CreateInstance<DpapiNGXmlDecryptor>(decryptorTypeName);
        }
        else if (typeof(DpapiXmlDecryptor).MatchName(typeNameToMatch, typeNameResolver))
        {
            return activator.CreateInstance<DpapiXmlDecryptor>(decryptorTypeName);
        }
        else if (typeof(EncryptedXmlDecryptor).MatchName(typeNameToMatch, typeNameResolver))
        {
            return activator.CreateInstance<EncryptedXmlDecryptor>(decryptorTypeName);
        }
        else if (typeof(NullXmlDecryptor).MatchName(typeNameToMatch, typeNameResolver))
        {
            return activator.CreateInstance<NullXmlDecryptor>(decryptorTypeName);
        }

        return activator.CreateInstance<IXmlDecryptor>(decryptorTypeName);
    }

    public static XElement? EncryptIfNecessary(this IXmlEncryptor encryptor, XElement element)
    {
        // If no encryption is necessary, return null.
        if (!DoesElementOrDescendentRequireEncryption(element))
        {
            return null;
        }

        // Deep copy the element (since we're going to mutate) and put
        // it into a document to guarantee it has a parent.
        var doc = new XDocument(new XElement(element));

        // We remove elements from the document as we encrypt them and perform
        // fix-up later. This keeps us from going into an infinite loop in
        // the case of a null encryptor (which returns its original input which
        // is still marked as 'requires encryption').
        var placeholderReplacements = new Dictionary<XElement, EncryptedXmlInfo>();

        while (true)
        {
            var elementWhichRequiresEncryption = doc.Descendants().FirstOrDefault(DoesSingleElementRequireEncryption);
            if (elementWhichRequiresEncryption == null)
            {
                // All encryption is finished.
                break;
            }

            // Encrypt the clone so that the encryptor doesn't inadvertently modify
            // the original document or other data structures.
            var clonedElementWhichRequiresEncryption = new XElement(elementWhichRequiresEncryption);
            var innerDoc = new XDocument(clonedElementWhichRequiresEncryption);
            var encryptedXmlInfo = encryptor.Encrypt(clonedElementWhichRequiresEncryption);
            CryptoUtil.Assert(encryptedXmlInfo != null, "IXmlEncryptor.Encrypt returned null.");

            // Put a placeholder into the original document so that we can continue our
            // search for elements which need to be encrypted.
            var newPlaceholder = new XElement("placeholder");
            placeholderReplacements[newPlaceholder] = encryptedXmlInfo;
            elementWhichRequiresEncryption.ReplaceWith(newPlaceholder);
        }

        // Finally, perform fixup.
        Debug.Assert(placeholderReplacements.Count > 0);
        foreach (var entry in placeholderReplacements)
        {
            // <enc:encryptedSecret decryptorType="{type}" xmlns:enc="{ns}">
            //   <element />
            // </enc:encryptedSecret>
            entry.Key.ReplaceWith(
                new XElement(XmlConstants.EncryptedSecretElementName,
                    new XAttribute(XmlConstants.DecryptorTypeAttributeName, entry.Value.DecryptorType.AssemblyQualifiedName!),
                    entry.Value.EncryptedElement));
        }
        return doc.Root;
    }

    /// <summary>
    /// Converts an <see cref="XElement"/> to a <see cref="Secret"/> so that it can be kept in memory
    /// securely or run through the DPAPI routines.
    /// </summary>
    public static Secret ToSecret(this XElement element)
    {
        const int DEFAULT_BUFFER_SIZE = 16 * 1024; // 16k buffer should be large enough to encrypt any realistic secret
        var memoryStream = new MemoryStream(DEFAULT_BUFFER_SIZE);
        element.Save(memoryStream);

        var underlyingBuffer = memoryStream.GetBuffer();
        fixed (byte* __unused__ = underlyingBuffer) // try to limit this moving around in memory while we allocate
        {
            try
            {
                return new Secret(new ArraySegment<byte>(underlyingBuffer, 0, checked((int)memoryStream.Length)));
            }
            finally
            {
                Array.Clear(underlyingBuffer, 0, underlyingBuffer.Length);
            }
        }
    }

    /// <summary>
    /// Converts a <see cref="Secret"/> back into an <see cref="XElement"/>.
    /// </summary>
    public static XElement ToXElement(this Secret secret)
    {
        var plaintextSecret = new byte[secret.Length];
        fixed (byte* __unused__ = plaintextSecret) // try to keep the GC from moving it around
        {
            try
            {
                secret.WriteSecretIntoBuffer(new ArraySegment<byte>(plaintextSecret));
                var memoryStream = new MemoryStream(plaintextSecret, writable: false);
                return XElement.Load(memoryStream);
            }
            finally
            {
                Array.Clear(plaintextSecret, 0, plaintextSecret.Length);
            }
        }
    }

    private static bool DoesElementOrDescendentRequireDecryption(XElement element)
    {
        return element.DescendantsAndSelf(XmlConstants.EncryptedSecretElementName).Any();
    }

    private static bool DoesElementOrDescendentRequireEncryption(XElement element)
    {
        return element.DescendantsAndSelf().Any(DoesSingleElementRequireEncryption);
    }

    private static bool DoesSingleElementRequireEncryption(XElement element)
    {
        return element.IsMarkedAsRequiringEncryption();
    }
}
