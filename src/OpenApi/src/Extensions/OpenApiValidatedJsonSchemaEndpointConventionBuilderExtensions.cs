// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0040 // The framework implements this experimental contract.

using System.Diagnostics.CodeAnalysis;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for enforcing validated JSON Schema endpoint contracts.
/// </summary>
[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public static class OpenApiValidatedJsonSchemaEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Adds endpoint-scoped JSON Schema evidence and runtime enforcement.
    /// </summary>
    public static TBuilder WithValidatedJsonSchema<TBuilder>(
        this TBuilder builder,
        OpenApiValidatedJsonSchemaRegistration registration)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(registration);

        builder.Add(endpointBuilder =>
        {
            OpenApiValidatedJsonSchemaEndpointPlan? plan = null;
            foreach (var metadata in endpointBuilder.Metadata)
            {
                if (metadata is OpenApiValidatedJsonSchemaEndpointPlan existingPlan)
                {
                    plan = existingPlan;
                    break;
                }
            }

            if (plan is null)
            {
                var next = endpointBuilder.RequestDelegate ??
                    throw new InvalidOperationException(Resources.ValidatedJsonSchemaRequestDelegateRequired);
                plan = new(next);
                endpointBuilder.Metadata.Add(plan);
                endpointBuilder.RequestDelegate = plan.ExecuteAsync;
            }

            plan.Add(registration);
            endpointBuilder.Metadata.Add(registration);
        });

        return builder;
    }
}

internal sealed class OpenApiValidatedJsonSchemaEndpointPlan
{
    private readonly RequestDelegate _next;
    private readonly List<OpenApiValidatedJsonSchemaRegistration> _inputs = [];
    private readonly List<OpenApiValidatedJsonSchemaRegistration> _outputs = [];
    private readonly ConcurrentBag<PooledBufferStream> _buffers = [];
    private long _maximumResponseSize;

    public OpenApiValidatedJsonSchemaEndpointPlan(RequestDelegate next)
    {
        _next = next;
    }

    public void Add(OpenApiValidatedJsonSchemaRegistration registration)
    {
        var registrations = registration.Purpose == OpenApiSchemaEvidencePurpose.Input ? _inputs : _outputs;
        foreach (var existing in registrations)
        {
            if (existing.Options.ResponseStatusCode == registration.Options.ResponseStatusCode &&
                string.Equals(existing.Options.ContentType, registration.Options.ContentType, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(Resources.FormatConflictingValidatedJsonSchemaRegistrations(
                    $"{registration.Purpose}:{registration.Options.ResponseStatusCode}:{registration.Options.ContentType}"));
            }
        }

        registrations.Add(registration);
        if (registration.Purpose == OpenApiSchemaEvidencePurpose.Output)
        {
            _maximumResponseSize = Math.Max(_maximumResponseSize, registration.Options.MaxPayloadSize);
        }
    }

    public Task ExecuteAsync(HttpContext context)
    {
        var input = SelectInputRegistration(_inputs, context.Request.ContentType);
        if (input is null)
        {
            return ExecuteResponseAsync(context);
        }

        if (context.Request.ContentLength is > 0 && context.Request.ContentLength > input.Options.MaxPayloadSize)
        {
            return WritePayloadTooLargeAsync(context, input.Options.MaxPayloadSize);
        }

        var buffer = RentBuffer(input.Options.MaxPayloadSize);
        ValueTask read;
        try
        {
            read = ReadRequestAsync(context.Request.Body, buffer, context.RequestAborted);
        }
        catch (PayloadLimitExceededException)
        {
            ReturnBuffer(buffer);
            return WritePayloadTooLargeAsync(context, input.Options.MaxPayloadSize);
        }
        catch
        {
            ReturnBuffer(buffer);
            throw;
        }
        if (!read.IsCompletedSuccessfully)
        {
            return AwaitRequestReadAsync(read, context, input, buffer);
        }

        return ContinueRequest(context, input, buffer);
    }

    private Task ContinueRequest(
        HttpContext context,
        OpenApiValidatedJsonSchemaRegistration input,
        PooledBufferStream buffer)
    {
        var originalBody = context.Request.Body;
        buffer.Position = 0;
        context.Request.Body = buffer;
        if (buffer.Length != 0 || !input.Options.AllowEmptyRequestBody)
        {
            ValueTask<OpenApiJsonSchemaValidationResult> validation;
            try
            {
                validation = input.Validator.ValidateAsync(
                    buffer.WrittenMemory,
                    input.ValidationContext,
                    context.RequestAborted);
            }
            catch
            {
                context.Request.Body = originalBody;
                ReturnBuffer(buffer);
                throw;
            }
            if (!validation.IsCompletedSuccessfully)
            {
                return AwaitRequestValidationAsync(validation, context, buffer, originalBody);
            }
            var result = validation.GetAwaiter().GetResult();
            if (!result.IsValid)
            {
                context.Request.Body = originalBody;
                ReturnBuffer(buffer);
                return WriteValidationProblemAsync(context, result);
            }
        }

        Task response;
        try
        {
            response = ExecuteResponseAsync(context);
        }
        catch
        {
            context.Request.Body = originalBody;
            ReturnBuffer(buffer);
            throw;
        }
        if (response.IsCompletedSuccessfully)
        {
            context.Request.Body = originalBody;
            ReturnBuffer(buffer);
            return Task.CompletedTask;
        }
        return AwaitRequestCompletionAsync(response, context, buffer, originalBody);
    }

    private Task ExecuteResponseAsync(HttpContext context)
    {
        if (_outputs.Count == 0)
        {
            return _next(context);
        }

        var originalFeature = context.Features.GetRequiredFeature<IHttpResponseBodyFeature>();
        var originalBody = originalFeature.Stream;
        var buffer = RentBuffer(_maximumResponseSize);
        context.Features.Set<IHttpResponseBodyFeature>(buffer);
        Task next;
        try
        {
            next = _next(context);
        }
        catch
        {
            context.Features.Set(originalFeature);
            ReturnBuffer(buffer);
            throw;
        }
        if (!next.IsCompletedSuccessfully)
        {
            return AwaitResponseAsync(next, context, buffer, originalFeature);
        }
        return CompleteResponse(context, buffer, originalFeature);
    }

    private Task CompleteResponse(
        HttpContext context,
        PooledBufferStream buffer,
        IHttpResponseBodyFeature originalFeature)
    {
        context.Features.Set(originalFeature);
        var originalBody = originalFeature.Stream;
        var output = SelectOutputRegistration(_outputs, context.Response.StatusCode, context.Response.ContentType);
        if (output is null || !HasJsonContentType(context.Response.ContentType) || IsNoContentResponse(context.Response.StatusCode))
        {
            return CopyResponse(context, buffer, originalBody);
        }

        if (buffer.Length > output.Options.MaxPayloadSize)
        {
            ReturnBuffer(buffer);
            return RejectResponseAsync(
                context,
                output,
                originalBody,
                Resources.FormatValidatedJsonSchemaPayloadTooLarge(output.Options.MaxPayloadSize));
        }

        ValueTask<OpenApiJsonSchemaValidationResult> validation;
        try
        {
            validation = output.Validator.ValidateAsync(
                buffer.WrittenMemory,
                output.ValidationContext,
                context.RequestAborted);
        }
        catch
        {
            ReturnBuffer(buffer);
            throw;
        }
        if (!validation.IsCompletedSuccessfully)
        {
            return AwaitResponseValidationAsync(validation, context, output, buffer, originalBody);
        }
        if (!validation.Result.IsValid)
        {
            ReturnBuffer(buffer);
            return RejectResponseAsync(context, output, originalBody, Resources.ValidatedJsonSchemaResponseInvalid);
        }

        return CopyResponse(context, buffer, originalBody);
    }

    private Task CopyResponse(HttpContext context, PooledBufferStream buffer, Stream originalBody)
    {
        ValueTask write;
        try
        {
            write = originalBody.WriteAsync(buffer.WrittenMemory, context.RequestAborted);
        }
        catch
        {
            ReturnBuffer(buffer);
            throw;
        }
        if (write.IsCompletedSuccessfully)
        {
            ReturnBuffer(buffer);
            return Task.CompletedTask;
        }
        return AwaitResponseCopyAsync(write, buffer);
    }

    private PooledBufferStream RentBuffer(long maximumLength)
    {
        if (!_buffers.TryTake(out var buffer))
        {
            buffer = new();
        }
        buffer.Reset(maximumLength);
        return buffer;
    }

    private void ReturnBuffer(PooledBufferStream buffer)
    {
        buffer.Clear();
        _buffers.Add(buffer);
    }

    private async Task AwaitRequestReadAsync(
        ValueTask read,
        HttpContext context,
        OpenApiValidatedJsonSchemaRegistration input,
        PooledBufferStream buffer)
    {
        try
        {
            await read;
        }
        catch (PayloadLimitExceededException)
        {
            ReturnBuffer(buffer);
            await WritePayloadTooLargeAsync(context, input.Options.MaxPayloadSize);
            return;
        }
        catch
        {
            ReturnBuffer(buffer);
            throw;
        }
        await ContinueRequest(context, input, buffer);
    }

    private async Task AwaitRequestValidationAsync(
        ValueTask<OpenApiJsonSchemaValidationResult> validation,
        HttpContext context,
        PooledBufferStream buffer,
        Stream originalBody)
    {
        OpenApiJsonSchemaValidationResult result;
        try
        {
            result = await validation;
        }
        catch
        {
            context.Request.Body = originalBody;
            ReturnBuffer(buffer);
            throw;
        }
        if (!result.IsValid)
        {
            context.Request.Body = originalBody;
            ReturnBuffer(buffer);
            await WriteValidationProblemAsync(context, result);
            return;
        }

        Task response;
        try
        {
            response = ExecuteResponseAsync(context);
        }
        catch
        {
            context.Request.Body = originalBody;
            ReturnBuffer(buffer);
            throw;
        }
        await AwaitRequestCompletionAsync(response, context, buffer, originalBody);
    }

    private async Task AwaitRequestCompletionAsync(
        Task response,
        HttpContext context,
        PooledBufferStream buffer,
        Stream originalBody)
    {
        try
        {
            await response;
        }
        finally
        {
            context.Request.Body = originalBody;
            ReturnBuffer(buffer);
        }
    }

    private async Task AwaitResponseAsync(
        Task next,
        HttpContext context,
        PooledBufferStream buffer,
        IHttpResponseBodyFeature originalFeature)
    {
        try
        {
            await next;
        }
        catch (PayloadLimitExceededException)
        {
            context.Features.Set(originalFeature);
            var selected = SelectOutputRegistration(_outputs, context.Response.StatusCode, context.Response.ContentType);
            ReturnBuffer(buffer);
            if (selected is null)
            {
                throw;
            }
            await RejectResponseAsync(
                context,
                selected,
                originalFeature.Stream,
                Resources.FormatValidatedJsonSchemaPayloadTooLarge(selected.Options.MaxPayloadSize));
            return;
        }
        catch
        {
            context.Features.Set(originalFeature);
            ReturnBuffer(buffer);
            throw;
        }

        await CompleteResponse(context, buffer, originalFeature);
    }

    private async Task AwaitResponseValidationAsync(
        ValueTask<OpenApiJsonSchemaValidationResult> validation,
        HttpContext context,
        OpenApiValidatedJsonSchemaRegistration output,
        PooledBufferStream buffer,
        Stream originalBody)
    {
        OpenApiJsonSchemaValidationResult result;
        try
        {
            result = await validation;
        }
        catch
        {
            ReturnBuffer(buffer);
            throw;
        }
        if (!result.IsValid)
        {
            ReturnBuffer(buffer);
            await RejectResponseAsync(context, output, originalBody, Resources.ValidatedJsonSchemaResponseInvalid);
            return;
        }
        await CopyResponse(context, buffer, originalBody);
    }

    private async Task AwaitResponseCopyAsync(ValueTask write, PooledBufferStream buffer)
    {
        try
        {
            await write;
        }
        finally
        {
            ReturnBuffer(buffer);
        }
    }

    private static OpenApiValidatedJsonSchemaRegistration? SelectInputRegistration(
        List<OpenApiValidatedJsonSchemaRegistration> registrations,
        string? contentType)
    {
        OpenApiValidatedJsonSchemaRegistration? selected = null;
        foreach (var registration in registrations)
        {
            if (registration.Purpose != OpenApiSchemaEvidencePurpose.Input ||
                !ContentTypeMatches(contentType, registration.Options.ContentType))
            {
                continue;
            }

            if (selected is not null)
            {
                throw new InvalidOperationException(Resources.FormatConflictingValidatedJsonSchemaRegistrations(
                    $"Input:{contentType}"));
            }
            selected = registration;
        }
        return selected;
    }

    private static OpenApiValidatedJsonSchemaRegistration? SelectOutputRegistration(
        List<OpenApiValidatedJsonSchemaRegistration> registrations,
        int statusCode,
        string? contentType)
    {
        OpenApiValidatedJsonSchemaRegistration? selected = null;
        foreach (var registration in registrations)
        {
            if (registration.Purpose != OpenApiSchemaEvidencePurpose.Output ||
                registration.Options.ResponseStatusCode is { } configuredStatus && configuredStatus != statusCode ||
                !ContentTypeMatches(contentType, registration.Options.ContentType))
            {
                continue;
            }

            if (selected is not null)
            {
                throw new InvalidOperationException(Resources.FormatConflictingValidatedJsonSchemaRegistrations(
                    $"Output:{statusCode}:{contentType}"));
            }
            selected = registration;
        }
        return selected;
    }

    private static ValueTask ReadRequestAsync(
        Stream source,
        PooledBufferStream destination,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var read = source.ReadAsync(destination.AvailableMemory, cancellationToken);
            if (!read.IsCompletedSuccessfully)
            {
                return ReadRequestSlowAsync(read, source, destination, cancellationToken);
            }
            var count = read.GetAwaiter().GetResult();
            if (count == 0)
            {
                return ValueTask.CompletedTask;
            }
            destination.Advance(count);
        }
    }

    private static async ValueTask ReadRequestSlowAsync(
        ValueTask<int> pendingRead,
        Stream source,
        PooledBufferStream destination,
        CancellationToken cancellationToken)
    {
        var count = await pendingRead;
        while (count != 0)
        {
            destination.Advance(count);
            count = await source.ReadAsync(destination.AvailableMemory, cancellationToken);
        }
    }

    private static async Task WriteValidationProblemAsync(
        HttpContext context,
        OpenApiJsonSchemaValidationResult validation)
    {
        var errors = validation.Errors!
            .GroupBy(static error => error.InstanceLocation, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key.Length == 0 ? "$" : group.Key,
                static group => group.Select(static error => error.Message).ToArray(),
                StringComparer.Ordinal);
        await Results.ValidationProblem(errors).ExecuteAsync(context);
    }

    private static async Task WritePayloadTooLargeAsync(HttpContext context, long limit)
    {
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        await Results.Problem(
            statusCode: StatusCodes.Status413PayloadTooLarge,
            detail: Resources.FormatValidatedJsonSchemaPayloadTooLarge(limit)).ExecuteAsync(context);
    }

    private static async Task RejectResponseAsync(
        HttpContext context,
        OpenApiValidatedJsonSchemaRegistration registration,
        Stream originalBody,
        string message)
    {
        var loggerFactory = (ILoggerFactory?)context.RequestServices.GetService(typeof(ILoggerFactory)) ??
            throw new InvalidOperationException(Resources.ValidatedJsonSchemaLoggerFactoryRequired);
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.OpenApi.ValidatedJsonSchema");
        logger.LogError(
            "Validated JSON response {SchemaIdentity} was suppressed: {Reason}",
            registration.Evidence.Identity,
            message);

        if (context.Response.HasStarted)
        {
            throw new InvalidOperationException(Resources.ValidatedJsonSchemaResponseStarted);
        }

        context.Response.Clear();
        context.Response.Body = originalBody;
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentLength = 0;
        await context.Response.CompleteAsync();
    }

    private static bool ContentTypeMatches(string? actual, string configured)
        => actual is not null &&
            actual.AsSpan().Trim().StartsWith(configured, StringComparison.OrdinalIgnoreCase) &&
            (actual.Length == configured.Length || actual[configured.Length] == ';');

    private static bool HasJsonContentType(string? contentType)
    {
        if (contentType is null)
        {
            return false;
        }

        var mediaType = contentType.AsSpan().Trim();
        var semicolon = mediaType.IndexOf(';');
        if (semicolon >= 0)
        {
            mediaType = mediaType[..semicolon].Trim();
        }
        return mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
            mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNoContentResponse(int statusCode)
        => statusCode is StatusCodes.Status204NoContent or StatusCodes.Status304NotModified ||
            statusCode is >= 100 and < 200;

    internal sealed class PooledBufferStream : Stream, IHttpResponseBodyFeature
    {
        private static readonly byte[] s_overflowBuffer = new byte[1];
        private byte[]? _buffer;
        private int _length;
        private int _position;
        private int _maximumLength;
        private readonly BufferPipeWriter _writer;

        public PooledBufferStream()
        {
            _writer = new(this);
        }

        public Memory<byte> AvailableMemory
        {
            get
            {
                if (_position == _maximumLength)
                {
                    return s_overflowBuffer;
                }
                EnsureCapacity(checked(_position + 1));
                var buffer = _buffer!;
                return buffer.AsMemory(
                    _position,
                    Math.Min(buffer.Length - _position, _maximumLength - _position));
            }
        }

        public ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _length);

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => _length;
        public override long Position
        {
            get => _position;
            set
            {
                if (value is < 0 or > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _position = (int)value;
            }
        }

        public void Advance(int count)
        {
            EnsureCapacity(checked(_position + count));
            _position += count;
            _length = Math.Max(_length, _position);
        }

        public void Reset(long maximumLength)
        {
            if (maximumLength is <= 0 or > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumLength));
            }
            _maximumLength = (int)maximumLength;
            _length = 0;
            _position = 0;
        }

        public void Clear()
        {
            _length = 0;
            _position = 0;
        }

        public override void Flush()
        {
        }

        Stream IHttpResponseBodyFeature.Stream => this;
        PipeWriter IHttpResponseBodyFeature.Writer => _writer;
        void IHttpResponseBodyFeature.DisableBuffering()
        {
        }
        Task IHttpResponseBodyFeature.StartAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
        Task IHttpResponseBodyFeature.SendFileAsync(
            string path,
            long offset,
            long? count,
            CancellationToken cancellationToken)
            => SendFileAsync(path, offset, count, cancellationToken);
        Task IHttpResponseBodyFeature.CompleteAsync()
            => Task.CompletedTask;

        private async Task SendFileAsync(
            string path,
            long offset,
            long? count,
            CancellationToken cancellationToken)
        {
            await using var file = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);
            file.Position = offset;
            if (count is { } length)
            {
                EnsureCapacity(checked(_position + length));
            }
            await file.CopyToAsync(this, cancellationToken);
        }
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public override int Read(byte[] buffer, int offset, int count)
            => Read(buffer.AsSpan(offset, count));
        public override int Read(Span<byte> buffer)
        {
            var count = Math.Min(buffer.Length, _length - _position);
            _buffer.AsSpan(_position, count).CopyTo(buffer);
            _position += count;
            return count;
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            Position = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _position + offset,
                SeekOrigin.End => _length + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin)),
            };
            return _position;
        }
        public override void SetLength(long value)
        {
            EnsureCapacity(value);
            _length = (int)value;
            if (_position > _length)
            {
                _position = _length;
            }
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            EnsureCapacity(checked(Position + count));
            Write(buffer.AsSpan(offset, count));
        }
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            EnsureCapacity(checked(Position + buffer.Length));
            buffer.CopyTo(_buffer.AsSpan(_position));
            Advance(buffer.Length);
        }
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Write(buffer.Span);
            return ValueTask.CompletedTask;
        }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Write(buffer, offset, count);
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _buffer is { } buffer)
            {
                _buffer = null;
                ArrayPool<byte>.Shared.Return(buffer);
            }
            base.Dispose(disposing);
        }

        private void EnsureCapacity(long length)
        {
            if (length > _maximumLength)
            {
                throw new PayloadLimitExceededException();
            }

            if (_buffer is null || length > _buffer.Length)
            {
                var size = Math.Min(_maximumLength, Math.Max((int)length, _buffer?.Length * 2 ?? 256));
                var replacement = ArrayPool<byte>.Shared.Rent(size);
                if (_buffer is { } buffer)
                {
                    buffer.AsSpan(0, _length).CopyTo(replacement);
                    ArrayPool<byte>.Shared.Return(buffer);
                }
                _buffer = replacement;
            }
        }

        private sealed class BufferPipeWriter(PooledBufferStream stream) : PipeWriter
        {
            public override bool CanGetUnflushedBytes => true;
            public override long UnflushedBytes => stream.Length;

            public override void Advance(int bytes)
                => stream.Advance(bytes);

            public override Memory<byte> GetMemory(int sizeHint = 0)
            {
                if (sizeHint > 0)
                {
                    stream.EnsureCapacity(checked(stream._position + sizeHint));
                }
                return stream.AvailableMemory;
            }

            public override Span<byte> GetSpan(int sizeHint = 0)
                => GetMemory(sizeHint).Span;

            public override void CancelPendingFlush()
            {
            }

            public override void Complete(Exception? exception = null)
            {
                if (exception is not null)
                {
                    throw exception;
                }
            }

            public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(new FlushResult(isCanceled: false, isCompleted: false));
            }
        }
    }

    private sealed class PayloadLimitExceededException : IOException;
}

internal static class OpenApiValidatedJsonSchemaEndpointExecutor
{
    public static Task ExecuteAsync(HttpContext context, RequestDelegate next)
    {
        var plan = new OpenApiValidatedJsonSchemaEndpointPlan(next);
        foreach (var registration in context.GetEndpoint()?.Metadata.GetOrderedMetadata<OpenApiValidatedJsonSchemaRegistration>() ?? [])
        {
            plan.Add(registration);
        }
        return plan.ExecuteAsync(context);
    }
}
