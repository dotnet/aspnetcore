// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Extensions;
using Microsoft.AspNet.Http.Features;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Http
{
    /// <summary>
    /// Provides extensions for HttpResponse exposing the SendFile extension.
    /// </summary>
    public static class SendFileResponseExtensions
    {
        /// <summary>
        /// Checks if the SendFile extension is supported.
        /// </summary>
        /// <param name="response"></param>
        /// <returns>True if sendfile feature exists in the response.</returns>
        public static bool SupportsSendFile([NotNull] this HttpResponse response)
        {
            return response.HttpContext.Features.Get<IHttpSendFileFeature>() != null;
        }

        /// <summary>
        /// Sends the given file using the SendFile extension.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static Task SendFileAsync([NotNull] this HttpResponse response, [NotNull] string fileName)
        {
            return response.SendFileAsync(fileName, 0, null, CancellationToken.None);
        }

        /// <summary>
        /// Sends the given file using the SendFile extension.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="fileName">The full or relative path to the file.</param>
        /// <param name="offset">The offset in the file.</param>
        /// <param name="count">The number of types to send, or null to send the remainder of the file.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task SendFileAsync([NotNull] this HttpResponse response, [NotNull] string fileName, long offset, long? count, CancellationToken cancellationToken)
        {
            var sendFile = response.HttpContext.Features.Get<IHttpSendFileFeature>();
            if (sendFile == null)
            {
                throw new NotSupportedException(Resources.Exception_SendFileNotSupported);
            }

            return sendFile.SendFileAsync(fileName, offset, count, cancellationToken);
        }
    }
}