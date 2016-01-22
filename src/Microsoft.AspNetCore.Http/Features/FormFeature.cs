// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Http.Features.Internal
{
    public class FormFeature : IFormFeature
    {
        private readonly HttpRequest _request;
        private Task<IFormCollection> _parsedFormTask;
        private IFormCollection _form;

        public FormFeature(IFormCollection form)
        {
            if (form == null)
            {
                throw new ArgumentNullException(nameof(form));
            }

            Form = form;
        }

        public FormFeature(HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            _request = request;
        }

        private MediaTypeHeaderValue ContentType
        {
            get
            {
                MediaTypeHeaderValue mt;
                MediaTypeHeaderValue.TryParse(_request.ContentType, out mt);
                return mt;
            }
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

                var contentType = ContentType;
                return HasApplicationFormContentType(contentType) || HasMultipartFormContentType(contentType);
            }
        }

        public IFormCollection Form
        {
            get { return _form; }
            set
            {
                _parsedFormTask = null;
                _form = value;
            }
        }

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

            // TODO: Issue #456 Avoid Sync-over-Async http://blogs.msdn.com/b/pfxteam/archive/2012/04/13/10293638.aspx
            // TODO: How do we prevent thread exhaustion?
            return ReadFormAsync().GetAwaiter().GetResult();
        }

        public Task<IFormCollection> ReadFormAsync() => ReadFormAsync(CancellationToken.None);

        public Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken)
        {
            // Avoid state machine and task allocation for repeated reads
            if (_parsedFormTask == null)
            {
                if (Form != null)
                {
                    _parsedFormTask = Task.FromResult(Form);
                }
                else
                {
                    _parsedFormTask = InnerReadFormAsync(cancellationToken);
                }
            }
            return _parsedFormTask;
        }

        private async Task<IFormCollection> InnerReadFormAsync(CancellationToken cancellationToken)
        {
            if (!HasFormContentType)
            {
                throw new InvalidOperationException("Incorrect Content-Type: " + _request.ContentType);
            }

            cancellationToken.ThrowIfCancellationRequested();

            _request.EnableRewind();

            FormCollection formFields = null;
            FormFileCollection files = null;

            // Some of these code paths use StreamReader which does not support cancellation tokens.
            using (cancellationToken.Register((state) => ((HttpContext)state).Abort(), _request.HttpContext))
            {
                var contentType = ContentType;
                // Check the content-type
                if (HasApplicationFormContentType(contentType))
                {
                    var encoding = FilterEncoding(contentType.Encoding);
                    formFields = new FormCollection(await FormReader.ReadFormAsync(_request.Body, encoding, cancellationToken));
                }
                else if (HasMultipartFormContentType(contentType))
                {
                    var formAccumulator = new KeyValueAccumulator();

                    var boundary = GetBoundary(contentType);
                    var multipartReader = new MultipartReader(boundary, _request.Body);
                    var section = await multipartReader.ReadNextSectionAsync(cancellationToken);
                    while (section != null)
                    {
                        ContentDispositionHeaderValue contentDisposition;
                        ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out contentDisposition);
                        if (HasFileContentDisposition(contentDisposition))
                        {
                            // Find the end
                            await section.Body.DrainAsync(cancellationToken);

                            var name = HeaderUtilities.RemoveQuotes(contentDisposition.Name) ?? string.Empty;
                            var fileName = HeaderUtilities.RemoveQuotes(contentDisposition.FileName) ?? string.Empty;

                            var file = new FormFile(_request.Body, section.BaseStreamOffset.Value, section.Body.Length, name, fileName)
                            {
                                Headers = new HeaderDictionary(section.Headers),
                            };
                            if (files == null)
                            {
                                files = new FormFileCollection();
                            }
                            files.Add(file);
                        }
                        else if (HasFormDataContentDisposition(contentDisposition))
                        {
                            // Content-Disposition: form-data; name="key"
                            //
                            // value

                            var key = HeaderUtilities.RemoveQuotes(contentDisposition.Name);
                            MediaTypeHeaderValue mediaType;
                            MediaTypeHeaderValue.TryParse(section.ContentType, out mediaType);
                            var encoding = FilterEncoding(mediaType?.Encoding);
                            using (var reader = new StreamReader(section.Body, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
                            {
                                var value = await reader.ReadToEndAsync();
                                formAccumulator.Append(key, value);
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false, "Unrecognized content-disposition for this section: " + section.ContentDisposition);
                        }

                        section = await multipartReader.ReadNextSectionAsync(cancellationToken);
                    }

                    if (formAccumulator.HasValues)
                    {
                        formFields = new FormCollection(formAccumulator.GetResults(), files);
                    }
                }
            }

            // Rewind so later readers don't have to.
            _request.Body.Seek(0, SeekOrigin.Begin);

            if (formFields != null)
            {
                Form = formFields;
            }
            else if (files != null)
            {
                Form = new FormCollection(null, files);
            }
            else
            {
                Form = FormCollection.Empty;
            }

            return Form;
        }

        private Encoding FilterEncoding(Encoding encoding)
        {
            // UTF-7 is insecure and should not be honored. UTF-8 will succeed for most cases.
            if (encoding == null || Encoding.UTF7.Equals(encoding))
            {
                return Encoding.UTF8;
            }
            return encoding;
        }

        private bool HasApplicationFormContentType(MediaTypeHeaderValue contentType)
        {
            // Content-Type: application/x-www-form-urlencoded; charset=utf-8
            return contentType != null && contentType.MediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
        }

        private bool HasMultipartFormContentType(MediaTypeHeaderValue contentType)
        {
            // Content-Type: multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq
            return contentType != null && contentType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase);
        }

        private bool HasFormDataContentDisposition(ContentDispositionHeaderValue contentDisposition)
        {
            // Content-Disposition: form-data; name="key";
            return contentDisposition != null && contentDisposition.DispositionType.Equals("form-data")
                && string.IsNullOrEmpty(contentDisposition.FileName) && string.IsNullOrEmpty(contentDisposition.FileNameStar);
        }

        private bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition)
        {
            // Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
            return contentDisposition != null && contentDisposition.DispositionType.Equals("form-data")
                && (!string.IsNullOrEmpty(contentDisposition.FileName) || !string.IsNullOrEmpty(contentDisposition.FileNameStar));
        }

        // Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
        // TODO: Limit the length of boundary we accept. The spec says ~70 chars.
        private static string GetBoundary(MediaTypeHeaderValue contentType)
        {
            var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary);
            if (string.IsNullOrWhiteSpace(boundary))
            {
                throw new InvalidOperationException("Missing content-type boundary.");
            }
            return boundary;
        }
    }
}
