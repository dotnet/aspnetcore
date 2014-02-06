// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.StaticFiles;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.HttpFeature;

namespace Microsoft.Owin
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
        /// <returns>True if sendfile.SendAsync is defined in the environment.</returns>
        public static bool SupportsSendFile(this HttpResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException("response");
            }
            return response.HttpContext.GetFeature<IHttpSendFile>() != null;
        }

        /// <summary>
        /// Sends the given file using the SendFile extension.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static Task SendFileAsync(this HttpResponse response, string fileName)
        {
            if (response == null)
            {
                throw new ArgumentNullException("response");
            }
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
        public static Task SendFileAsync(this HttpResponse response, string fileName, long offset, long? count, CancellationToken cancellationToken)
        {
            if (response == null)
            {
                throw new ArgumentNullException("response");
            }
            IHttpSendFile sendFile = response.HttpContext.GetFeature<IHttpSendFile>();
            if (sendFile == null)
            {
                throw new NotSupportedException(Resources.Exception_SendFileNotSupported);
            }

            return sendFile.SendFileAsync(fileName, offset, count, cancellationToken);
        }
    }
}
