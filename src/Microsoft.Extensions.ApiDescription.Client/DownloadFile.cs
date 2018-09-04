// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Task = System.Threading.Tasks.Task;
using Utilities = Microsoft.Build.Utilities;

namespace GenerationTasks
{
    /// <summary>
    /// Downloads a file.
    /// </summary>
    public class DownloadFile : Utilities.Task, ICancelableTask
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>
        /// The URI to download.
        /// </summary>
        [Required]
        public string Uri { get; set; }

        /// <summary>
        /// Destination for the downloaded file. If the file already exists, it is not re-downloaded unless
        /// <see cref="Overwrite"/> is true.
        /// </summary>
        [Required]
        public string DestinationPath { get; set; }

        /// <summary>
        /// Should <see cref="DestinationPath"/> be overwritten. When <c>true</c>, the file is downloaded and its hash
        /// compared to the existing file. If those hashes do not match (or <see cref="DestinationPath"/> does not
        /// exist), <see cref="DestinationPath"/> is overwritten.
        /// </summary>
        public bool Overwrite { get; set; }

        /// <summary>
        /// The maximum amount of time in seconds to allow for downloading the file. Defaults to 2 minutes.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 60 * 2;

        /// <inheritdoc/>
        public void Cancel() => _cts.Cancel();

        /// <inheritdoc/>
        public override bool Execute() => ExecuteAsync().Result;

        public async Task<bool> ExecuteAsync()
        {
            if (string.IsNullOrEmpty(Uri))
            {
                Log.LogError("Uri parameter must not be null or empty.");
                return false;
            }

            if (string.IsNullOrEmpty(Uri))
            {
                Log.LogError("DestinationPath parameter must not be null or empty.");
                return false;
            }

            var builder = new UriBuilder(Uri);
            if (!string.Equals(System.Uri.UriSchemeHttp, builder.Scheme, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(System.Uri.UriSchemeHttps, builder.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                Log.LogError($"{nameof(Uri)} parameter does not have scheme {System.Uri.UriSchemeHttp} or " +
                    $"{System.Uri.UriSchemeHttps}.");
                return false;
            }

            await DownloadFileAsync(Uri, DestinationPath, Overwrite,  _cts.Token, TimeoutSeconds, Log);

            return !Log.HasLoggedErrors;
        }

        private static async Task DownloadFileAsync(
            string uri,
            string destinationPath,
            bool overwrite,
            CancellationToken cancellationToken,
            int timeoutSeconds,
            TaskLoggingHelper log)
        {
            var destinationExists = File.Exists(destinationPath);
            if (destinationExists && !overwrite)
            {
                log.LogMessage($"Not downloading '{uri}' to overwrite existing file '{destinationPath}'.");
                return;
            }

            log.LogMessage($"Downloading '{uri}' to '{destinationPath}'.");

            using (var httpClient = new HttpClient
            {
            })
            {
                await DownloadFileCore.DownloadAsync(
                    uri,
                    destinationPath,
                    httpClient,
                    new LogWrapper(log),
                    cancellationToken,
                    timeoutSeconds);
            }
        }
    }
}
