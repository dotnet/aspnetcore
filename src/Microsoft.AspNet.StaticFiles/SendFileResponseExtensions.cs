// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.HttpFeature;

namespace Microsoft.AspNet.StaticFiles
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
            return response.HttpContext.GetFeature<IHttpSendFileFeature>() != null;
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
            var sendFile = response.HttpContext.GetFeature<IHttpSendFileFeature>();
            if (sendFile == null)
            {
                throw new NotSupportedException(Resources.Exception_SendFileNotSupported);
            }

            return sendFile.SendFileAsync(fileName, offset, count, cancellationToken);
        }
    }
}
