// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Default implementation for <see cref="IFormFeature"/>.
/// </summary>
public class FormFeature : IFormFeature
{
    private readonly HttpRequest _request;
    private readonly FormOptions _options;
    private Task<IFormCollection>? _parsedFormTask;
    private IFormCollection? _form;

    /// <summary>
    /// Initializes a new instance of <see cref="FormFeature"/>.
    /// </summary>
    /// <param name="form">The <see cref="IFormCollection"/> to use as the backing store.</param>
    public FormFeature(IFormCollection form)
    {
        ArgumentNullException.ThrowIfNull(form);

        Form = form;
        _request = default!;
        _options = FormOptions.Default;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FormFeature"/>.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    public FormFeature(HttpRequest request)
        : this(request, FormOptions.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FormFeature"/>.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="options">The <see cref="FormOptions"/>.</param>
    public FormFeature(HttpRequest request, FormOptions options)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(options);

        _request = request;
        _options = options;
    }

    private MediaTypeHeaderValue? ContentType
    {
        get
        {
            _ = MediaTypeHeaderValue.TryParse(_request.ContentType, out var mt);
            return mt;
        }
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public IFormCollection? Form
    {
        get { return _form; }
        set
        {
            _parsedFormTask = null;
            _form = value;
        }
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public Task<IFormCollection> ReadFormAsync() => ReadFormAsync(CancellationToken.None);

    /// <inheritdoc />
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

        if (_request.ContentLength == 0)
        {
            return FormCollection.Empty;
        }

        if (_options.BufferBody)
        {
            _request.EnableRewind(_options.MemoryBufferThreshold, _options.BufferBodyLengthLimit);
        }

        FormCollection? formFields = null;
        FormFileCollection? files = null;

        // Some of these code paths use StreamReader which does not support cancellation tokens.
        using (cancellationToken.Register((state) => ((HttpContext)state!).Abort(), _request.HttpContext))
        {
            var contentType = ContentType;
            // Check the content-type
            if (HasApplicationFormContentType(contentType))
            {
                var encoding = FilterEncoding(contentType.Encoding);
                var formReader = new FormPipeReader(_request.BodyReader, encoding)
                {
                    ValueCountLimit = _options.ValueCountLimit,
                    KeyLengthLimit = _options.KeyLengthLimit,
                    ValueLengthLimit = _options.ValueLengthLimit,
                };
                formFields = new FormCollection(await formReader.ReadFormAsync(cancellationToken));
            }
            else if (HasMultipartFormContentType(contentType))
            {
                var formAccumulator = new KeyValueAccumulator();
                var sectionCount = 0;

                var boundary = GetBoundary(contentType, _options.MultipartBoundaryLengthLimit);
                var multipartReader = new MultipartReader(boundary, _request.Body)
                {
                    HeadersCountLimit = _options.MultipartHeadersCountLimit,
                    HeadersLengthLimit = _options.MultipartHeadersLengthLimit,
                    BodyLengthLimit = _options.MultipartBodyLengthLimit,
                };
                var section = await multipartReader.ReadNextSectionAsync(cancellationToken);
                while (section != null)
                {
                    sectionCount++;
                    if (sectionCount > _options.ValueCountLimit)
                    {
                        throw new InvalidDataException($"Form value count limit {_options.ValueCountLimit} exceeded.");
                    }
                    // Parse the content disposition here and pass it further to avoid reparsings
                    if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
                    {
                        throw new InvalidDataException("Form section has invalid Content-Disposition value: " + section.ContentDisposition);
                    }

                    if (contentDisposition.IsFileDisposition())
                    {
                        var fileSection = new FileMultipartSection(section, contentDisposition);

                        // Enable buffering for the file if not already done for the full body
                        section.EnableRewind(
                            _request.HttpContext.Response.RegisterForDispose,
                            _options.MemoryBufferThreshold, _options.MultipartBodyLengthLimit);

                        // Find the end
                        await section.Body.DrainAsync(cancellationToken);

                        var name = fileSection.Name;
                        var fileName = fileSection.FileName;

                        FormFile file;
                        if (section.BaseStreamOffset.HasValue)
                        {
                            // Relative reference to buffered request body
                            file = new FormFile(_request.Body, section.BaseStreamOffset.GetValueOrDefault(), section.Body.Length, name, fileName);
                        }
                        else
                        {
                            // Individually buffered file body
                            file = new FormFile(section.Body, 0, section.Body.Length, name, fileName);
                        }
                        file.Headers = new HeaderDictionary(section.Headers);

                        if (files == null)
                        {
                            files = new FormFileCollection();
                        }
                        files.Add(file);
                    }
                    else if (contentDisposition.IsFormDisposition())
                    {
                        var formDataSection = new FormMultipartSection(section, contentDisposition);

                        // Content-Disposition: form-data; name="key"
                        //
                        // value

                        // Do not limit the key name length here because the multipart headers length limit is already in effect.
                        var key = formDataSection.Name;
                        var value = await formDataSection.GetValueAsync(cancellationToken);

                        formAccumulator.Append(key, value);
                    }
                    else
                    {
                        // Ignore form sections with invalid content disposition
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
        if (_request.Body.CanSeek)
        {
            _request.Body.Seek(0, SeekOrigin.Begin);
        }

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

    private static Encoding FilterEncoding(Encoding? encoding)
    {
        // UTF-7 is insecure and should not be honored. UTF-8 will succeed for most cases.
        // https://learn.microsoft.com/en-us/dotnet/core/compatibility/syslib-warnings/syslib0001
        if (encoding == null || encoding.CodePage == 65000)
        {
            return Encoding.UTF8;
        }
        return encoding;
    }

    private static bool HasApplicationFormContentType([NotNullWhen(true)] MediaTypeHeaderValue? contentType)
    {
        // Content-Type: application/x-www-form-urlencoded; charset=utf-8
        return contentType != null && contentType.MediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasMultipartFormContentType([NotNullWhen(true)] MediaTypeHeaderValue? contentType)
    {
        // Content-Type: multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq
        return contentType != null && contentType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase);
    }

    // Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
    // The spec says 70 characters is a reasonable limit.
    private static string GetBoundary(MediaTypeHeaderValue contentType, int lengthLimit)
    {
        var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary);
        if (StringSegment.IsNullOrEmpty(boundary))
        {
            throw new InvalidDataException("Missing content-type boundary.");
        }
        if (boundary.Length > lengthLimit)
        {
            throw new InvalidDataException($"Multipart boundary length limit {lengthLimit} exceeded.");
        }
        return boundary.ToString();
    }
}
