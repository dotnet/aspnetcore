// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.AspNetCore.DataProtection.AzureStorage
{
    /// <summary>
    /// An <see cref="IXmlRepository"/> which is backed by Azure Blob Storage.
    /// </summary>
    /// <remarks>
    /// Instances of this type are thread-safe.
    /// </remarks>
    public sealed class AzureBlobXmlRepository : IXmlRepository
    {
        private const int ConflictMaxRetries = 5;
        private static readonly TimeSpan ConflictBackoffPeriod = TimeSpan.FromMilliseconds(200);

        private static readonly XName RepositoryElementName = "repository";

        private readonly Func<ICloudBlob> _blobRefFactory;
        private readonly Random _random;
        private BlobData _cachedBlobData;

        /// <summary>
        /// Creates a new instance of the <see cref="AzureBlobXmlRepository"/>.
        /// </summary>
        /// <param name="blobRefFactory">A factory which can create <see cref="ICloudBlob"/>
        /// instances. The factory must be thread-safe for invocation by multiple
        /// concurrent threads, and each invocation must return a new object.</param>
        public AzureBlobXmlRepository(Func<ICloudBlob> blobRefFactory)
        {
            if (blobRefFactory == null)
            {
                throw new ArgumentNullException(nameof(blobRefFactory));
            }

            _blobRefFactory = blobRefFactory;
            _random = new Random();
        }

        /// <inheritdoc />
        public IReadOnlyCollection<XElement> GetAllElements()
        {
            var blobRef = CreateFreshBlobRef();

            // Shunt the work onto a ThreadPool thread so that it's independent of any
            // existing sync context or other potentially deadlock-causing items.

            var elements = Task.Run(() => GetAllElementsAsync(blobRef)).GetAwaiter().GetResult();
            return new ReadOnlyCollection<XElement>(elements);
        }

        /// <inheritdoc />
        public void StoreElement(XElement element, string friendlyName)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            var blobRef = CreateFreshBlobRef();

            // Shunt the work onto a ThreadPool thread so that it's independent of any
            // existing sync context or other potentially deadlock-causing items.

            Task.Run(() => StoreElementAsync(blobRef, element)).GetAwaiter().GetResult();
        }

        private XDocument CreateDocumentFromBlob(byte[] blob)
        {
            using (var memoryStream = new MemoryStream(blob))
            {
                var xmlReaderSettings = new XmlReaderSettings()
                {
                    DtdProcessing = DtdProcessing.Prohibit, IgnoreProcessingInstructions = true
                };

                using (var xmlReader = XmlReader.Create(memoryStream, xmlReaderSettings))
                {
                    return XDocument.Load(xmlReader);
                }
            }
        }

        private ICloudBlob CreateFreshBlobRef()
        {
            // ICloudBlob instances aren't thread-safe, so we need to make sure we're working
            // with a fresh instance that won't be mutated by another thread.

            var blobRef = _blobRefFactory();
            if (blobRef == null)
            {
                throw new InvalidOperationException("The ICloudBlob factory method returned null.");
            }

            return blobRef;
        }

        private async Task<IList<XElement>> GetAllElementsAsync(ICloudBlob blobRef)
        {
            var data = await GetLatestDataAsync(blobRef);

            if (data == null)
            {
                // no data in blob storage
                return new XElement[0];
            }

            // The document will look like this:
            //
            // <root>
            //   <child />
            //   <child />
            //   ...
            // </root>
            //
            // We want to return the first-level child elements to our caller.

            var doc = CreateDocumentFromBlob(data.BlobContents);
            return doc.Root.Elements().ToList();
        }

        private async Task<BlobData> GetLatestDataAsync(ICloudBlob blobRef)
        {
            // Set the appropriate AccessCondition based on what we believe the latest
            // file contents to be, then make the request.

            var latestCachedData = Volatile.Read(ref _cachedBlobData); // local ref so field isn't mutated under our feet
            var accessCondition = (latestCachedData != null)
                ? AccessCondition.GenerateIfNoneMatchCondition(latestCachedData.ETag)
                : null;

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await blobRef.DownloadToStreamAsync(
                        target: memoryStream,
                        accessCondition: accessCondition,
                        options: null,
                        operationContext: null);

                    // At this point, our original cache either didn't exist or was outdated.
                    // We'll update it now and return the updated value;

                    latestCachedData = new BlobData()
                    {
                        BlobContents = memoryStream.ToArray(),
                        ETag = blobRef.Properties.ETag
                    };

                }
                Volatile.Write(ref _cachedBlobData, latestCachedData);
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 304)
            {
                // 304 Not Modified
                // Thrown when we already have the latest cached data.
                // This isn't an error; we'll return our cached copy of the data.
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 404)
            {
                // 404 Not Found
                // Thrown when no file exists in storage.
                // This isn't an error; we'll delete our cached copy of data.

                latestCachedData = null;
                Volatile.Write(ref _cachedBlobData, latestCachedData);
            }

            return latestCachedData;
        }

        private int GetRandomizedBackoffPeriod()
        {
            // returns a TimeSpan in the range [0.8, 1.0) * ConflictBackoffPeriod
            // not used for crypto purposes
            var multiplier = 0.8 + (_random.NextDouble() * 0.2);
            return (int) (multiplier * ConflictBackoffPeriod.Ticks);
        }

        private async Task StoreElementAsync(ICloudBlob blobRef, XElement element)
        {
            // holds the last error in case we need to rethrow it
            ExceptionDispatchInfo lastError = null;

            for (var i = 0; i < ConflictMaxRetries; i++)
            {
                if (i > 1)
                {
                    // If multiple conflicts occurred, wait a small period of time before retrying
                    // the operation so that other writers can make forward progress.
                    await Task.Delay(GetRandomizedBackoffPeriod());
                }

                if (i > 0)
                {
                    // If at least one conflict occurred, make sure we have an up-to-date
                    // view of the blob contents.
                    await GetLatestDataAsync(blobRef);
                }

                // Merge the new element into the document. If no document exists,
                // create a new default document and inject this element into it.

                var latestData = Volatile.Read(ref _cachedBlobData);
                var doc = (latestData != null)
                    ? CreateDocumentFromBlob(latestData.BlobContents)
                    : new XDocument(new XElement(RepositoryElementName));
                doc.Root.Add(element);

                // Turn this document back into a byte[].

                var serializedDoc = new MemoryStream();
                doc.Save(serializedDoc, SaveOptions.DisableFormatting);

                // Generate the appropriate precondition header based on whether or not
                // we believe data already exists in storage.

                AccessCondition accessCondition;
                if (latestData != null)
                {
                    accessCondition = AccessCondition.GenerateIfMatchCondition(blobRef.Properties.ETag);
                }
                else
                {
                    accessCondition = AccessCondition.GenerateIfNotExistsCondition();
                    blobRef.Properties.ContentType = "application/xml; charset=utf-8"; // set content type on first write
                }

                try
                {
                    // Send the request up to the server.

                    var serializedDocAsByteArray = serializedDoc.ToArray();

                    await blobRef.UploadFromByteArrayAsync(
                        buffer: serializedDocAsByteArray,
                        index: 0,
                        count: serializedDocAsByteArray.Length,
                        accessCondition: accessCondition,
                        options: null,
                        operationContext: null);

                    // If we got this far, success!
                    // We can update the cached view of the remote contents.

                    Volatile.Write(ref _cachedBlobData, new BlobData()
                    {
                        BlobContents = serializedDocAsByteArray,
                        ETag = blobRef.Properties.ETag // was updated by Upload routine
                    });

                    return;
                }
                catch (StorageException ex)
                    when (ex.RequestInformation.HttpStatusCode == 409 || ex.RequestInformation.HttpStatusCode == 412)
                {
                    // 409 Conflict
                    // This error is rare but can be thrown in very special circumstances,
                    // such as if the blob in the process of being created. We treat it
                    // as equivalent to 412 for the purposes of retry logic.

                    // 412 Precondition Failed
                    // We'll get this error if another writer updated the repository and we
                    // have an outdated view of its contents. If this occurs, we'll just
                    // refresh our view of the remote contents and try again up to the max
                    // retry limit.

                    lastError = ExceptionDispatchInfo.Capture(ex);
                }
            }

            // if we got this far, something went awry
            lastError.Throw();
        }

        private sealed class BlobData
        {
            internal byte[] BlobContents;
            internal string ETag;
        }
    }
}
