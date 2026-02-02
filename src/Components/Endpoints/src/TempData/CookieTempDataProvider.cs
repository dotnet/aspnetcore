// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed partial class CookieTempDataProvider : ITempDataProvider
{
    public const string CookieName = ".AspNetCore.Components.TempData";
    private const string Purpose = "Microsoft.AspNetCore.Components.CookieTempDataProviderToken";
    private readonly IDataProtector _dataProtector;
    private readonly ISpanDataProtector? _spanDataProtector;
    private readonly ITempDataSerializer _tempDataSerializer;
    private readonly RazorComponentsServiceOptions _options;
    private readonly ChunkingCookieManager _chunkingCookieManager;
    private readonly ILogger<CookieTempDataProvider> _logger;

    public CookieTempDataProvider(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<RazorComponentsServiceOptions> options,
        ITempDataSerializer tempDataSerializer,
        ILogger<CookieTempDataProvider> logger)
    {
        _dataProtector = dataProtectionProvider.CreateProtector(Purpose);
        _spanDataProtector = _dataProtector as ISpanDataProtector;
        _tempDataSerializer = tempDataSerializer;
        _options = options.Value;
        _chunkingCookieManager = new ChunkingCookieManager();
        _logger = logger;
    }

    public IDictionary<string, object?> LoadTempData(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var cookieName = _options.TempDataCookie.Name ?? CookieName;

        try
        {
            if (!context.Request.Cookies.ContainsKey(cookieName))
            {
                Log.TempDataCookieNotFound(_logger, cookieName);
                return ReadOnlyDictionary<string, object?>.Empty;
            }
            var serializedDataFromCookie = _chunkingCookieManager.GetRequestCookie(context, cookieName);
            if (serializedDataFromCookie is null)
            {
                return ReadOnlyDictionary<string, object?>.Empty;
            }

            byte[]? rentedDecodeBuffer = null;
            var maxDecodedSize = Base64Url.GetMaxDecodedLength(serializedDataFromCookie.Length);
            var decodeBuffer = maxDecodedSize <= 256
                ? stackalloc byte[256]
                : (rentedDecodeBuffer = ArrayPool<byte>.Shared.Rent(maxDecodedSize));

            try
            {
                var decodeStatus = Base64Url.DecodeFromChars(serializedDataFromCookie, decodeBuffer, out _, out var bytesWritten);
                var protectedBytes = decodeBuffer[..bytesWritten];
                Dictionary<string, JsonElement>? dataFromCookie;

                if (_spanDataProtector is not null)
                {
                    var unprotectBuffer = new RefPooledArrayBufferWriter<byte>(stackalloc byte[256]);
                    _spanDataProtector.Unprotect(protectedBytes, ref unprotectBuffer);
                    dataFromCookie = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(unprotectBuffer.WrittenSpan);
                }
                else
                {
                    var unprotectedBytes = _dataProtector.Unprotect(protectedBytes.ToArray());
                    dataFromCookie = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(unprotectedBytes);
                }

                if (dataFromCookie is null)
                {
                    return ReadOnlyDictionary<string, object?>.Empty;
                }
                var convertedData = _tempDataSerializer.DeserializeData(dataFromCookie);
                Log.TempDataCookieLoadSuccess(_logger, cookieName);
                return convertedData;
            }
            finally
            {
                if (rentedDecodeBuffer is not null)
                {
                    ArrayPool<byte>.Shared.Return(rentedDecodeBuffer);
                }
            }
        }
        catch (Exception ex)
        {
            Log.TempDataCookieLoadFailure(_logger, cookieName, ex);

            var cookieOptions = _options.TempDataCookie.Build(context);
            SetCookiePath(context, cookieOptions);
            context.Response.Cookies.Delete(cookieName, cookieOptions);
            return ReadOnlyDictionary<string, object?>.Empty;
        }
    }

    public void SaveTempData(HttpContext context, IDictionary<string, object?> values)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var kvp in values)
        {
            if (kvp.Value is not null && !_tempDataSerializer.CanSerialize(kvp.Value.GetType()))
            {
                throw new InvalidOperationException($"TempData cannot store values of type '{kvp.Value.GetType()}'.");
            }
        }

        var cookieName = _options.TempDataCookie.Name ?? CookieName;
        var cookieOptions = _options.TempDataCookie.Build(context);
        SetCookiePath(context, cookieOptions);

        if (values.Count == 0)
        {
            _chunkingCookieManager.DeleteCookie(context, cookieName, cookieOptions);
            return;
        }

        var bytes = _tempDataSerializer.SerializeData(values);
        var protectedBytes = _dataProtector.Protect(bytes);
        var encodedValue = Base64Url.EncodeToString(protectedBytes);
        _chunkingCookieManager.AppendResponseCookie(context, cookieName, encodedValue, cookieOptions);
        Log.TempDataCookieSaveSuccess(_logger, cookieName);
    }

    private void SetCookiePath(HttpContext httpContext, CookieOptions cookieOptions)
    {
        if (!string.IsNullOrEmpty(_options.TempDataCookie.Path))
        {
            cookieOptions.Path = _options.TempDataCookie.Path;
        }
        else
        {
            var pathBase = httpContext.Request.PathBase.ToString();
            if (!string.IsNullOrEmpty(pathBase))
            {
                cookieOptions.Path = pathBase;
            }
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "The temp data cookie {CookieName} was not found.", EventName = "TempDataCookieNotFound")]
        public static partial void TempDataCookieNotFound(ILogger logger, string cookieName);

        [LoggerMessage(2, LogLevel.Warning, "The temp data cookie {CookieName} could not be loaded.", EventName = "TempDataCookieLoadFailure")]
        public static partial void TempDataCookieLoadFailure(ILogger logger, string cookieName, Exception exception);

        [LoggerMessage(3, LogLevel.Debug, "The temp data cookie {CookieName} was successfully saved.", EventName = "TempDataCookieSaveSuccess")]
        public static partial void TempDataCookieSaveSuccess(ILogger logger, string cookieName);

        [LoggerMessage(4, LogLevel.Debug, "The temp data cookie {CookieName} was successfully loaded.", EventName = "TempDataCookieLoadSuccess")]
        public static partial void TempDataCookieLoadSuccess(ILogger logger, string cookieName);
    }
}
