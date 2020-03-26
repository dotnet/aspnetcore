// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RepoTasks
{
    public class DownloadFile : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string Uri { get; set; }

        /// <summary>
        /// If this field is set and the task fail to download the file from `Uri`, with a NotFound
        /// status, it will try to download the file from `PrivateUri`.
        /// </summary>
        public string PrivateUri { get; set; }

        /// <summary>
        /// Suffix for the private URI in base64 form (for SAS compatibility)
        /// </summary>
        public string PrivateUriSuffix { get; set; }

        public int MaxRetries { get; set; } = 5;

        [Required]
        public string DestinationPath { get; set; }

        public bool Overwrite { get; set; }

        public override bool Execute()
        {
            return ExecuteAsync().GetAwaiter().GetResult();
        }

        private async System.Threading.Tasks.Task<bool> ExecuteAsync()
        {
            string destinationDir = Path.GetDirectoryName(DestinationPath);
            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            if (File.Exists(DestinationPath) && !Overwrite)
            {
                return true;
            }

            const string FileUriProtocol = "file://";

            if (Uri.StartsWith(FileUriProtocol, StringComparison.Ordinal))
            {
                var filePath = Uri.Substring(FileUriProtocol.Length);
                Log.LogMessage($"Copying '{filePath}' to '{DestinationPath}'");
                File.Copy(filePath, DestinationPath);
                return true;
            }

            List<string> errorMessages = new List<string>();
            bool? downloadStatus = await DownloadWithRetriesAsync(Uri, DestinationPath, errorMessages);

            if (downloadStatus == false && !string.IsNullOrEmpty(PrivateUri))
            {
                string uriSuffix = "";
                if (!string.IsNullOrEmpty(PrivateUriSuffix))
                {
                    var uriSuffixBytes = System.Convert.FromBase64String(PrivateUriSuffix);
                    uriSuffix = System.Text.Encoding.UTF8.GetString(uriSuffixBytes);
                }
                downloadStatus = await DownloadWithRetriesAsync($"{PrivateUri}{uriSuffix}", DestinationPath, errorMessages);
            }

            if (downloadStatus != true)
            {
                foreach (var error in errorMessages)
                {
                    Log.LogError(error);
                }
            }

            return downloadStatus == true;
        }

        /// <summary>
        /// Attempt to download file from `source` with retries when response error is different of FileNotFound and Success.
        /// </summary>
        /// <param name="source">URL to the file to be downloaded.</param>
        /// <param name="target">Local path where to put the downloaded file.</param>
        /// <returns>true: Download Succeeded. false: Download failed with 404. null: Download failed but is retriable.</returns>
        private async Task<bool?> DownloadWithRetriesAsync(string source, string target, List<string> errorMessages)
        {
            Random rng = new Random();

            Log.LogMessage(MessageImportance.High, $"Attempting download '{source}' to '{target}'");

            using (var httpClient = new HttpClient())
            {
                for (int retryNumber = 0; retryNumber < MaxRetries; retryNumber++)
                {
                    try
                    {
                        var httpResponse = await httpClient.GetAsync(source);

                        Log.LogMessage(MessageImportance.High, $"{source} -> {httpResponse.StatusCode}");

                        // The Azure Storage REST API returns '400 - Bad Request' in some cases
                        // where the resource is not found on the storage.
                        // https://docs.microsoft.com/en-us/rest/api/storageservices/common-rest-api-error-codes
                        if (httpResponse.StatusCode == HttpStatusCode.NotFound ||
                            httpResponse.ReasonPhrase.IndexOf("The requested URI does not represent any resource on the server.", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            errorMessages.Add($"Problems downloading file from '{source}'. Does the resource exist on the storage? {httpResponse.StatusCode} : {httpResponse.ReasonPhrase}");
                            return false;
                        }

                        httpResponse.EnsureSuccessStatusCode();

                        using (var outStream = File.Create(target))
                        {
                            await httpResponse.Content.CopyToAsync(outStream);
                        }

                        Log.LogMessage(MessageImportance.High, $"returning true {source} -> {httpResponse.StatusCode}");
                        return true;
                    }
                    catch (Exception e)
                    {
                        Log.LogMessage(MessageImportance.High, $"returning error in {source} ");
                        errorMessages.Add($"Problems downloading file from '{source}'. {e.Message} {e.StackTrace}");
                        File.Delete(target);
                    }

                    await System.Threading.Tasks.Task.Delay(rng.Next(1000, 10000));
                }
            }

            Log.LogMessage(MessageImportance.High, $"giving up {source} ");
            errorMessages.Add($"Giving up downloading the file from '{source}' after {MaxRetries} retries.");
            return null;
        }
    }
}