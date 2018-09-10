// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.ApiDescription.Client
{
    internal static class DownloadFileCore
    {
        public static async Task DownloadAsync(
            string uri,
            string destinationPath,
            HttpClient httpClient,
            ILogWrapper log,
            CancellationToken cancellationToken,
            int timeoutSeconds)
        {
            // Timeout if the response has not begun within 1 minute
            httpClient.Timeout = TimeSpan.FromMinutes(1);

            var destinationExists = File.Exists(destinationPath);
            var reachedCopy = false;
            try
            {
                using (var response = await httpClient.GetAsync(uri, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();
                    cancellationToken.ThrowIfCancellationRequested();

                    using (var responseStreamTask = response.Content.ReadAsStreamAsync())
                    {
                        var finished = await Task.WhenAny(
                            responseStreamTask,
                            Task.Delay(TimeSpan.FromSeconds(timeoutSeconds)));

                        if (!ReferenceEquals(responseStreamTask, finished))
                        {
                            throw new TimeoutException($"Download failed to complete in {timeoutSeconds} seconds.");
                        }

                        var responseStream = await responseStreamTask;
                        if (destinationExists)
                        {
                            // Check hashes before using the downloaded information.
                            var downloadHash = GetHash(responseStream);
                            responseStream.Position = 0L;

                            byte[] destinationHash;
                            using (var destinationStream = File.OpenRead(destinationPath))
                            {
                                destinationHash = GetHash(destinationStream);
                            }

                            var sameHashes = downloadHash.LongLength == destinationHash.LongLength;
                            for (var i = 0L; sameHashes && i < downloadHash.LongLength; i++)
                            {
                                sameHashes = downloadHash[i] == destinationHash[i];
                            }

                            if (sameHashes)
                            {
                                log.LogInformational($"Not overwriting existing and matching file '{destinationPath}'.");
                                return;
                            }
                        }
                        else
                        {
                            // May need to create directory to hold the file.
                            var destinationDirectory = Path.GetDirectoryName(destinationPath);
                            if (!(string.IsNullOrEmpty(destinationDirectory) || Directory.Exists(destinationDirectory)))
                            {
                                Directory.CreateDirectory(destinationDirectory);
                            }
                        }

                        // Create or overwrite the destination file.
                        reachedCopy = true;
                        using (var outStream = File.Create(destinationPath))
                        {
                            responseStream.CopyTo(outStream);
                        }
                    }
                }
            }
            catch (HttpRequestException ex) when (destinationExists)
            {
                if (ex.InnerException is SocketException socketException)
                {
                    log.LogWarning($"Unable to download {uri}, socket error code '{socketException.SocketErrorCode}'.");
                }
                else
                {
                    log.LogWarning($"Unable to download {uri}: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Downloading '{uri}' failed.");
                log.LogError(ex, showStackTrace: true);
                if (reachedCopy)
                {
                    File.Delete(destinationPath);
                }
            }
        }

        private static byte[] GetHash(Stream stream)
        {
            using (var algorithm = SHA256.Create())
            {
                return algorithm.ComputeHash(stream);
            }
        }
    }
}
