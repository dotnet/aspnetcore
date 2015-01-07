// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Represents an <see cref="ActionResult"/> that when executed will
    /// write a file as the response.
    /// </summary>
    public abstract class FileResult : ActionResult
    {
        private string _fileDownloadName;

        /// <summary>
        /// Creates a new <see cref="FileResult"/> instance with
        /// the provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="contentType">The Content-Type header of the response.</param>
        protected FileResult([NotNull] string contentType)
        {
            ContentType = contentType;
        }

        /// <summary>
        /// Gets the Content-Type header value that will be written to the response.
        /// </summary>
        public string ContentType { get; private set; }

        /// <summary>
        /// Gets the file name that will be used in the Content-Disposition header of the response.
        /// </summary>
        public string FileDownloadName
        {
            get { return _fileDownloadName ?? string.Empty; }
            set { _fileDownloadName = value; }
        }

        /// <inheritdoc />
        public override Task ExecuteResultAsync([NotNull] ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.ContentType = ContentType;

            if (!string.IsNullOrEmpty(FileDownloadName))
            {
                // From RFC 2183, Sec. 2.3:
                // The sender may want to suggest a filename to be used if the entity is
                // detached and stored in a separate file. If the receiving MUA writes
                // the entity to a file, the suggested filename should be used as a
                // basis for the actual filename, where possible.
                var headerValue = ContentDispositionUtil.GetHeaderValue(FileDownloadName);
                context.HttpContext.Response.Headers.Set("Content-Disposition", headerValue);
            }

            // We aren't flowing the cancellation token appropiately, see
            // https://github.com/aspnet/Mvc/issues/743 for details.
            return WriteFileAsync(response, CancellationToken.None);
        }

        /// <summary>
        /// Writes the file to the response.
        /// </summary>
        /// <param name="response">
        /// The <see cref="HttpResponse"/> where the file will be written
        /// </param>
        /// <param name="cancellation">The <see cref="CancellationToken"/>to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> that will complete when the file has been written to the response.
        /// </returns>
        protected abstract Task WriteFileAsync(HttpResponse response, CancellationToken cancellation);

        // This is a temporary implementation until we have the right abstractions in HttpAbstractions.
        internal static class ContentDispositionUtil
        {
            private const string HexDigits = "0123456789ABCDEF";

            private static void AddByteToStringBuilder(byte b, StringBuilder builder)
            {
                builder.Append('%');

                var i = b;
                AddHexDigitToStringBuilder(i >> 4, builder);
                AddHexDigitToStringBuilder(i % 16, builder);
            }

            private static void AddHexDigitToStringBuilder(int digit, StringBuilder builder)
            {
                builder.Append(HexDigits[digit]);
            }

            private static string CreateRfc2231HeaderValue(string filename)
            {
                var builder = new StringBuilder("attachment; filename*=UTF-8''");

                var filenameBytes = Encoding.UTF8.GetBytes(filename);
                foreach (var b in filenameBytes)
                {
                    if (IsByteValidHeaderValueCharacter(b))
                    {
                        builder.Append((char)b);
                    }
                    else
                    {
                        AddByteToStringBuilder(b, builder);
                    }
                }

                return builder.ToString();
            }

            public static string GetHeaderValue(string fileName)
            {
                // If fileName contains any Unicode characters, encode according
                // to RFC 2231 (with clarifications from RFC 5987)
                foreach (var c in fileName)
                {
                    if ((int)c > 127)
                    {
                        return CreateRfc2231HeaderValue(fileName);
                    }
                }

                return CreateNonUnicodeCharactersHeaderValue(fileName);
            }

            private static string CreateNonUnicodeCharactersHeaderValue(string fileName)
            {
                var escapedFileName = EscapeFileName(fileName);
                return string.Format("attachment; filename={0}", escapedFileName);
            }

            private static string EscapeFileName(string fileName)
            {
                var hasToBeQuoted = false;

                // We can't break the loop earlier because we need to check the
                // whole name for \n, in which case we need to provide a special
                // encoding.
                for (var i = 0; i < fileName.Length; i++)
                {
                    if (fileName[i] == '\n')
                    {
                        // See RFC 2047 for more details
                        return GetRfc2047Base64EncodedWord(fileName);
                    }

                    // Control characters = (octets 0 - 31) and DEL (127)
                    if (char.IsControl(fileName[i]))
                    {
                        hasToBeQuoted = true;
                    }

                    switch (fileName[i])
                    {
                        case '(':
                        case ')':
                        case '<':
                        case '>':
                        case '@':
                        case ',':
                        case ';':
                        case ':':
                        case '\\':
                        case '/':
                        case '[':
                        case ']':
                        case '?':
                        case '=':
                        case '{':
                        case '}':
                        case ' ':
                        case '\t':
                        case '"':
                            hasToBeQuoted = true;
                            break;
                        default:
                            break;
                    }
                }

                return hasToBeQuoted ? QuoteFileName(fileName) : fileName;
            }

            private static string QuoteFileName(string fileName)
            {
                var builder = new StringBuilder();
                builder.Append("\"");

                for (var i = 0; i < fileName.Length; i++)
                {
                    switch (fileName[i])
                    {
                        case '\\':
                            // Escape \
                            builder.Append("\\\\");
                            break;
                        case '"':
                            // Escape "
                            builder.Append("\\\"");
                            break;
                        default:
                            builder.Append(fileName[i]);
                            break;
                    }
                }

                builder.Append("\"");
                return builder.ToString();
            }

            private static string GetRfc2047Base64EncodedWord(string fileName)
            {
                // See RFC 2047 for details. Section 8 for examples.
                const string charset = "utf-8";
                // B means Base64
                const string encoding = "B";

                var fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                var base64EncodedFileName = Convert.ToBase64String(fileNameBytes);

                // Encoded words are defined as "=?{charset}?{encoding}?{encpoded value}?="
                return string.Format("\"=?{0}?{1}?{2}?=\"", charset, encoding, base64EncodedFileName);
            }

            // Application of RFC 2231 Encoding to Hypertext Transfer Protocol (HTTP) Header Fields, sec. 3.2
            // http://greenbytes.de/tech/webdav/draft-reschke-rfc2231-in-http-latest.html
            private static bool IsByteValidHeaderValueCharacter(byte b)
            {
                if ((byte)'0' <= b && b <= (byte)'9')
                {
                    return true; // is digit
                }
                if ((byte)'a' <= b && b <= (byte)'z')
                {
                    return true; // lowercase letter
                }
                if ((byte)'A' <= b && b <= (byte)'Z')
                {
                    return true; // uppercase letter
                }

                switch (b)
                {
                    case (byte)'-':
                    case (byte)'.':
                    case (byte)'_':
                    case (byte)'~':
                    case (byte)':':
                    case (byte)'!':
                    case (byte)'$':
                    case (byte)'&':
                    case (byte)'+':
                        return true;
                }

                return false;
            }
        }
    }
}