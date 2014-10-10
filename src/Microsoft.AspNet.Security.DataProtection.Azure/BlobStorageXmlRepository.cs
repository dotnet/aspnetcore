// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Xml.Linq;
using Microsoft.AspNet.Security.DataProtection.Repositories;
using Microsoft.Framework.OptionsModel;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.AspNet.Security.DataProtection.Azure
{
    /// <summary>
    /// An XML repository backed by Azure blob storage.
    /// </summary>
    public class BlobStorageXmlRepository : IXmlRepository
    {
        private const int MAX_NUM_UPDATE_ATTEMPTS = 10;

        internal static readonly XNamespace XmlNamespace = XNamespace.Get("http://www.asp.net/dataProtection/2014/azure");
        internal static readonly XName KeyRingElementName = XmlNamespace.GetName("keyRing");

        public BlobStorageXmlRepository([NotNull] IOptions<BlobStorageXmlRepositoryOptions> optionsAccessor)
        {
            Directory = optionsAccessor.Options.Directory;
            CryptoUtil.Assert(Directory != null, "Directory != null");
        }

        protected CloudBlobDirectory Directory
        {
            get;
            private set;
        }

        // IXmlRepository objects are supposed to be thread-safe, but CloudBlockBlob
        // instances do not meet this criterion. We'll create them on-demand so that each
        // thread can have its own instance that doesn't impact others.
        private CloudBlockBlob GetKeyRingBlockBlobReference()
        {
            return Directory.GetBlockBlobReference("keyring.xml");
        }

        public virtual IReadOnlyCollection<XElement> GetAllElements()
        {
            var blobRef = GetKeyRingBlockBlobReference();
            XDocument document = ReadDocumentFromStorage(blobRef);
            return document?.Root.Elements().ToArray() ?? new XElement[0];
        }

        private XDocument ReadDocumentFromStorage(CloudBlockBlob blobRef)
        {
            // Try downloading from Azure storage
            using (var memoryStream = new MemoryStream())
            {
                try
                {
                    blobRef.DownloadToStream(memoryStream);
                }
                catch (StorageException ex) if (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    // 404s are not a fatal error - empty keyring
                    return null;
                }

                // Rewind the memory stream and read it into an XDocument
                memoryStream.Position = 0;
                XDocument document = XDocument.Load(memoryStream);

                // Format checks
                CryptoUtil.Assert(document.Root.Name == KeyRingElementName, "TODO: Unknown element.");
                CryptoUtil.Assert((int)document.Root.Attribute("version") == 1, "TODO: Unknown version.");
                return document;
            }
        }

        public virtual void StoreElement([NotNull] XElement element, string friendlyName)
        {
            ExceptionDispatchInfo lastException = null;

            // To perform a transactional update of keyring.xml, we first need to get
            // the original contents of the blob.
            var blobRef = GetKeyRingBlockBlobReference();

            for (int i = 0; i < MAX_NUM_UPDATE_ATTEMPTS; i++)
            {
                AccessCondition updateAccessCondition;
                XDocument document = ReadDocumentFromStorage(blobRef);

                // Inject the new element into the existing <keyRing> root.
                if (document != null)
                {
                    document.Root.Add(element);

                    // only update if the contents haven't changed (prevents overwrite)
                    updateAccessCondition = AccessCondition.GenerateIfMatchCondition(blobRef.Properties.ETag);
                }
                else
                {
                    document = new XDocument(
                        new XElement(KeyRingElementName,
                            new XAttribute("version", 1),
                            element));

                    // only update if the file doesn't exist (prevents overwrite)
                    updateAccessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                }

                // Write the updated document back out
                MemoryStream memoryStream = new MemoryStream();
                document.Save(memoryStream);
                try
                {
                    blobRef.UploadFromByteArray(memoryStream.GetBuffer(), 0, checked((int)memoryStream.Length), accessCondition: updateAccessCondition);
                    return; // success!
                }
                catch (StorageException ex)
                {
                    switch ((HttpStatusCode)ex.RequestInformation.HttpStatusCode)
                    {
                        // If we couldn't update the blob due to a conflict on the server, try again.
                        case HttpStatusCode.Conflict:
                        case HttpStatusCode.PreconditionFailed:
                            lastException = ExceptionDispatchInfo.Capture(ex);
                            continue;

                        default:
                            throw;
                    }
                }
            }

            // If we got this far, too many conflicts occurred while trying to update the blob.
            // Just bail.
            lastException.Throw();
        }
    }
}
