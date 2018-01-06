// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace RepoTasks
{
    /// <summary>
    /// Publish files to an Azure storage blob
    /// </summary>
    public class PublishToAzureBlob : Microsoft.Build.Utilities.Task, ICancelableTask
    {
        private CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>
        /// The files to publish.
        /// </summary>
        [Required]
        public ITaskItem[] Files { get; set; }

        /// <summary>
        /// The Azure blob storage account name.
        /// </summary>
        [Required]
        public string AccountName { get; set; }

        /// <summary>
        /// The SAS token used to write to Azure.
        /// </summary>
        [Required]
        public string SharedAccessToken { get; set; }

        /// <summary>
        /// The Azure blob storage container name
        /// </summary>
        [Required]
        public string ContainerName { get; set; }

        public void Cancel() => _cts.Cancel();

        public override bool Execute()
            => ExecuteAsync().Result;

        private async Task<bool> ExecuteAsync()
        {
            var connectionString = $"BlobEndpoint=https://{AccountName}.blob.core.windows.net;SharedAccessSignature={SharedAccessToken}";

            var account = CloudStorageAccount.Parse(connectionString);
            var client = account.CreateCloudBlobClient();
            var container = client.GetContainerReference(ContainerName);

            var ctx = new OperationContext();

            foreach (var item in Files)
            {
                // normalize slashes
                var dest = item.GetMetadata("RelativeBlobPath")
                    .Replace('\\', '/')
                    .Replace("//", "/");
                var contentType = item.GetMetadata("ContentType");
                var cacheControl = item.GetMetadata("CacheControl");

                if (string.IsNullOrEmpty(dest))
                {
                    Log.LogError($"Item {item.ItemSpec} is missing required metadata 'RelativeBlobPath'");
                    return false;
                }

                var blob = container.GetBlockBlobReference(dest);

                if (!string.IsNullOrEmpty(cacheControl))
                {
                    blob.Properties.CacheControl = cacheControl;
                }

                if (!string.IsNullOrEmpty(contentType))
                {
                    blob.Properties.ContentType = contentType;
                }

                Log.LogMessage(MessageImportance.High, $"Publishing {item.ItemSpec} to https://{AccountName}.blob.core.windows.net/{ContainerName}/{dest}");

                var accessCondition = bool.TryParse(item.GetMetadata("Overwrite"), out var overwrite) && overwrite
                    ? AccessCondition.GenerateEmptyCondition()
                    : AccessCondition.GenerateIfNotExistsCondition();

                await blob.UploadFromFileAsync(item.ItemSpec, accessCondition, new BlobRequestOptions(), ctx, _cts.Token);
            }

            return true;
        }
    }
}
