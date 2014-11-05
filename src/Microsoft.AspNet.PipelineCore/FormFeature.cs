// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.PipelineCore.Collections;
using Microsoft.AspNet.WebUtilities;

namespace Microsoft.AspNet.PipelineCore
{
    public class FormFeature : IFormFeature
    {
        private readonly HttpRequest _request;

        public FormFeature([NotNull] IFormCollection form)
        {
            Form = form;
        }

        public FormFeature([NotNull] HttpRequest request)
        {
            _request = request;
        }

        public bool HasFormContentType
        {
            get
            {
                // Set directly
                if (Form != null)
                {
                    return true;
                }

                return HasApplicationFormContentType() || HasMultipartFormContentType();
            }
        }

        public IFormCollection Form { get; set; }

        public IFormCollection ReadForm()
        {
            if (Form != null)
            {
                return Form;
            }

            if (!HasFormContentType)
            {
                throw new InvalidOperationException("Incorrect Content-Type: " + _request.ContentType);
            }

            // TODO: How do we prevent thread exhaustion?
            return ReadFormAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public async Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken)
        {
            if (Form != null)
            {
                return Form;
            }

            if (!HasFormContentType)
            {
                throw new InvalidOperationException("Incorrect Content-Type: " + _request.ContentType);
            }

            cancellationToken.ThrowIfCancellationRequested();

            _request.EnableRewind();

            IDictionary<string, string[]> formFields = null;
            var files = new FormFileCollection();

            // Some of these code paths use StreamReader which does not support cancellation tokens.
            using (cancellationToken.Register(_request.HttpContext.Abort))
            {
                // Check the content-type
                if (HasApplicationFormContentType())
                {
                    // TODO: Read the charset from the content-type header after we get strongly typed headers
                    formFields = await FormReader.ReadFormAsync(_request.Body, cancellationToken);
                }
                else if (HasMultipartFormContentType())
                {
                    var formAccumulator = new KeyValueAccumulator<string, string>(StringComparer.OrdinalIgnoreCase);

                    var boundary = GetBoundary(_request.ContentType);
                    var multipartReader = new MultipartReader(boundary, _request.Body);
                    var section = await multipartReader.ReadNextSectionAsync(cancellationToken);
                    while (section != null)
                    {
                        var headers = new HeaderDictionary(section.Headers);
                        var contentDisposition = headers["Content-Disposition"];
                        if (HasFileContentDisposition(contentDisposition))
                        {
                            // Find the end
                            await section.Body.DrainAsync(cancellationToken);

                            var file = new FormFile(_request.Body, section.BaseStreamOffset.Value, section.Body.Length)
                            {
                                Headers = headers,
                            };
                            files.Add(file);
                        }
                        else if (HasFormDataContentDisposition(contentDisposition))
                        {
                            // Content-Disposition: form-data; name="key"
                            //
                            // value

                            // TODO: Strongly typed headers will take care of this
                            var offset = contentDisposition.IndexOf("name=") + "name=".Length;
                            var key = contentDisposition.Substring(offset + 1, contentDisposition.Length - offset - 2); // Remove quotes

                            // TODO: Read the charset from the content-disposition header after we get strongly typed headers
                            using (var reader = new StreamReader(section.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
                            {
                                var value = await reader.ReadToEndAsync();
                                formAccumulator.Append(key, value);
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false, "Unrecognized content-disposition for this section: " + contentDisposition);
                        }

                        section = await multipartReader.ReadNextSectionAsync(cancellationToken);
                    }

                    formFields = formAccumulator.GetResults();
                }
            }

            Form = new FormCollection(formFields, files);
            return Form;
        }

        private bool HasApplicationFormContentType()
        {
            // TODO: Strongly typed headers will take care of this for us
            // Content-Type: application/x-www-form-urlencoded; charset=utf-8
            var contentType = _request.ContentType;
            return !string.IsNullOrEmpty(contentType) && contentType.IndexOf("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool HasMultipartFormContentType()
        {
            // TODO: Strongly typed headers will take care of this for us
            // Content-Type: multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq
            var contentType = _request.ContentType;
            return !string.IsNullOrEmpty(contentType) && contentType.IndexOf("multipart/form-data", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool HasFormDataContentDisposition(string contentDisposition)
        {
            // TODO: Strongly typed headers will take care of this for us
            // Content-Disposition: form-data; name="key";
            return !string.IsNullOrEmpty(contentDisposition) && contentDisposition.Contains("form-data") && !contentDisposition.Contains("filename=");
        }

        private bool HasFileContentDisposition(string contentDisposition)
        {
            // TODO: Strongly typed headers will take care of this for us
            // Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
            return !string.IsNullOrEmpty(contentDisposition) && contentDisposition.Contains("form-data") && contentDisposition.Contains("filename=");
        }

        // Content-Type: multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq
        private static string GetBoundary(string contentType)
        {
            // TODO: Strongly typed headers will take care of this for us
            // TODO: Limit the length of boundary we accept. The spec says ~70 chars.
            var elements = contentType.Split(' ');
            var element = elements.Where(entry => entry.StartsWith("boundary=")).First();
            var boundary = element.Substring("boundary=".Length);
            // Remove quotes
            if (boundary.Length >= 2 && boundary[0] == '"' && boundary[boundary.Length - 1] == '"')
            {
                boundary = boundary.Substring(1, boundary.Length - 2);
            }
            return boundary;
        }
    }
}
