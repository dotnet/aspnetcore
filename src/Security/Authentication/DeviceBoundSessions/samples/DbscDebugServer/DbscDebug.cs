// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Formats.Cbor;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DbscDebugServer;

/// <summary>Well-known scheme, cookie, and purpose names used by the debug tooling.</summary>
public static class DbscNames
{
    public const string Source = "Application";
    public const string Refresh = "Application.Dbsc.Refresh";
    public const string Session = "Application.Dbsc.Session";

    public const string SourceCookie = ".AspNetCore.Application";
    public const string RefreshCookie = ".AspNetCore.Application.Dbsc.Refresh";
    public const string SessionCookie = ".AspNetCore.Application.Dbsc.Session";

    // The challenge protector is domain-separated by flow: registration challenges and refresh
    // challenges are protected under distinct purposes, so a challenge from one flow can never be
    // decrypted (or confused) as the other.
    public const string RegistrationChallengePurpose = "Microsoft.AspNetCore.Authentication.DeviceBoundSessions.Challenge.Registration.v1";
    public const string RefreshChallengePurpose = "Microsoft.AspNetCore.Authentication.DeviceBoundSessions.Challenge.Refresh.v1";

    public static string? SchemeForCookie(string name) => name switch
    {
        SourceCookie => Source,
        RefreshCookie => Refresh,
        SessionCookie => Session,
        _ => null,
    };
}

/// <summary>
/// Mutable, process-wide debug state for the sample: the current session TTL and a ring
/// buffer of decoded HTTP exchanges that the dashboard polls.
/// </summary>
public sealed class DbscDebugState
{
    public const int MaxEntries = 400;

    private readonly object _lock = new();
    private readonly LinkedList<DebugExchange> _exchanges = new();
    private long _nextId;
    private long _ttlTicks = TimeSpan.FromSeconds(300).Ticks;

    // Signaled whenever the log changes, so long-poll requests can wake without busy polling.
    private volatile TaskCompletionSource _signal = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TimeSpan SessionTtl
    {
        get => TimeSpan.FromTicks(Interlocked.Read(ref _ttlTicks));
        set => Interlocked.Exchange(ref _ttlTicks, value.Ticks);
    }

    private DebugCookie? _latestRefreshCookie;

    /// <summary>
    /// The most recently observed refresh cookie (decoded). The refresh cookie is path-scoped to
    /// /.well-known/dbsc, so the dashboard's own /debug/state polls never receive it; the capture
    /// middleware stashes the latest copy here from the DBSC traffic that does carry it.
    /// </summary>
    public DebugCookie? LatestRefreshCookie
    {
        get { lock (_lock) { return _latestRefreshCookie; } }
        set { lock (_lock) { _latestRefreshCookie = value; } }
    }

    public DebugExchange Add(DebugExchange exchange)
    {
        lock (_lock)
        {
            exchange.Id = ++_nextId;
            _exchanges.AddLast(exchange);
            while (_exchanges.Count > MaxEntries)
            {
                _exchanges.RemoveFirst();
            }
        }
        SignalChange();
        return exchange;
    }

    public (long LastId, List<DebugExchange> Entries) Since(long sinceId)
    {
        lock (_lock)
        {
            var list = new List<DebugExchange>();
            foreach (var e in _exchanges)
            {
                if (e.Id > sinceId)
                {
                    list.Add(e);
                }
            }
            return (_nextId, list);
        }
    }

    /// <summary>
    /// Long-poll wait: completes immediately if entries newer than <paramref name="sinceId"/>
    /// already exist, otherwise waits until the log changes or <paramref name="timeout"/> elapses.
    /// </summary>
    public async Task<(long LastId, List<DebugExchange> Entries)> WaitForChangesAsync(
        long sinceId, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var current = Since(sinceId);
        if (current.Entries.Count > 0 || current.LastId < sinceId)
        {
            return current;
        }

        var signal = _signal;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);
        try
        {
            await signal.Task.WaitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timed out with no change — return the current (likely empty) snapshot.
        }

        return Since(sinceId);
    }

    public void ClearLog()
    {
        lock (_lock)
        {
            _exchanges.Clear();
        }
        SignalChange();
    }

    private void SignalChange()
    {
        var previous = Interlocked.Exchange(ref _signal, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        previous.TrySetResult();
    }
}

// ===== Serializable models surfaced to the dashboard =====

public sealed class DebugExchange
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("time")] public string Time { get; set; } = "";
    [JsonPropertyName("method")] public string Method { get; set; } = "";
    [JsonPropertyName("path")] public string Path { get; set; } = "";
    [JsonPropertyName("status")] public int Status { get; set; }
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("authenticated")] public bool Authenticated { get; set; }
    [JsonPropertyName("durationMs")] public double DurationMs { get; set; }
    [JsonPropertyName("requestCookies")] public List<DebugCookie>? RequestCookies { get; set; }
    [JsonPropertyName("setCookies")] public List<DebugCookie>? SetCookies { get; set; }
    [JsonPropertyName("proof")] public DebugJwt? Proof { get; set; }
    [JsonPropertyName("registrationHeader")] public string? RegistrationHeader { get; set; }
    [JsonPropertyName("challengeHeader")] public string? ChallengeHeader { get; set; }
    [JsonPropertyName("decodedChallenge")] public DebugChallenge? DecodedChallenge { get; set; }
    [JsonPropertyName("sessionConfig")] public JsonNode? SessionConfig { get; set; }
}

public sealed class DebugCookie
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("scheme")] public string? Scheme { get; set; }
    [JsonPropertyName("valueLength")] public int ValueLength { get; set; }
    [JsonPropertyName("attributes")] public string? Attributes { get; set; }
    [JsonPropertyName("deleted")] public bool Deleted { get; set; }
    [JsonPropertyName("decoded")] public DecodedTicket? Decoded { get; set; }
}

public sealed class DecodedTicket
{
    [JsonPropertyName("principal")] public string? Principal { get; set; }
    [JsonPropertyName("claims")] public List<string> Claims { get; set; } = new();
    [JsonPropertyName("items")] public Dictionary<string, string> Items { get; set; } = new();
    [JsonPropertyName("issuedUtc")] public string? IssuedUtc { get; set; }
    [JsonPropertyName("expiresUtc")] public string? ExpiresUtc { get; set; }
}

public sealed class DebugJwt
{
    [JsonPropertyName("header")] public JsonNode? Header { get; set; }
    [JsonPropertyName("payload")] public JsonNode? Payload { get; set; }
    [JsonPropertyName("signaturePresent")] public bool SignaturePresent { get; set; }
    [JsonPropertyName("decodedJti")] public DebugChallenge? DecodedJti { get; set; }
}

public sealed class DebugChallenge
{
    [JsonPropertyName("kind")] public string Kind { get; set; } = "";
    [JsonPropertyName("claimUid")] public string? ClaimUid { get; set; }
    [JsonPropertyName("sessionId")] public string? SessionId { get; set; }
    [JsonPropertyName("valid")] public bool Valid { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
}

/// <summary>
/// Decodes DBSC artifacts for the dashboard: proof JWTs (plain base64url JSON), Data-Protection
/// cookie tickets (via each scheme's <see cref="CookieAuthenticationOptions.TicketDataFormat"/>),
/// and the time-limited Data-Protection challenge payloads (CBOR).
/// </summary>
public static class DbscDecoder
{
    public static List<DebugCookie>? DecodeRequestCookies(HttpContext context)
    {
        var result = new List<DebugCookie>();
        foreach (var name in new[] { DbscNames.SourceCookie, DbscNames.RefreshCookie, DbscNames.SessionCookie })
        {
            var value = ReadChunkedCookie(context.Request.Cookies, name);
            if (value is null)
            {
                continue;
            }

            var scheme = DbscNames.SchemeForCookie(name);
            result.Add(new DebugCookie
            {
                Name = name,
                Scheme = scheme,
                ValueLength = value.Length,
                Decoded = scheme is null ? null : TryDecodeTicket(context, scheme, value),
            });
        }
        return result.Count > 0 ? result : null;
    }

    public static List<DebugCookie>? DecodeSetCookies(HttpContext context)
    {
        var raw = context.Response.Headers.SetCookie;
        if (raw.Count == 0)
        {
            return null;
        }

        // Reassemble chunked Set-Cookie values (name=chunks-N + nameC1..CN).
        var parsed = new List<(string Name, string Value, string? Attrs)>();
        var byName = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in raw)
        {
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }
            var segs = line.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var first = segs[0];
            var eq = first.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }
            var name = first[..eq];
            var value = first[(eq + 1)..];
            var attrs = segs.Length > 1 ? string.Join("; ", segs[1..]) : null;
            parsed.Add((name, value, attrs));
            byName[name] = value;
        }

        var result = new List<DebugCookie>();
        foreach (var (name, value, attrs) in parsed)
        {
            // Skip the chunk-part cookies themselves; they are folded into the base entry.
            if (IsChunkPart(name))
            {
                continue;
            }

            var deleted = value.Length == 0 || (attrs?.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase) ?? false);
            var fullValue = value.StartsWith("chunks-", StringComparison.Ordinal) ? Reassemble(name, value, byName) : value;
            var scheme = DbscNames.SchemeForCookie(name);

            result.Add(new DebugCookie
            {
                Name = name,
                Scheme = scheme,
                ValueLength = fullValue.Length,
                Attributes = attrs,
                Deleted = deleted,
                Decoded = (deleted || scheme is null) ? null : TryDecodeTicket(context, scheme, fullValue),
            });
        }
        return result.Count > 0 ? result : null;
    }

    /// <summary>Parses a JSON response body (the DBSC Session Instruction) for display, or null.</summary>
    public static JsonNode? TryParseJsonBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }
        try
        {
            return JsonNode.Parse(body);
        }
        catch
        {
            return null;
        }
    }

    public static DebugJwt? DecodeProof(HttpContext context)
    {
        var raw = context.Request.Headers["Secure-Session-Response"].ToString();
        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }

        raw = raw.Trim().Trim('"');
        var parts = raw.Split('.');
        if (parts.Length is < 2 or > 3)
        {
            return null;
        }

        var header = TryParseJsonSegment(parts[0]);
        var payload = TryParseJsonSegment(parts[1]);
        if (header is null || payload is null)
        {
            return null;
        }

        var jwt = new DebugJwt
        {
            Header = header,
            Payload = payload,
            SignaturePresent = parts.Length == 3 && parts[2].Length > 0,
        };

        if (payload["jti"]?.GetValue<string>() is { Length: > 0 } jti)
        {
            jwt.DecodedJti = DecodeChallenge(context, jti);
        }
        return jwt;
    }

    public static DebugChallenge? DecodeChallengeHeader(HttpContext context, string? challengeHeader)
    {
        if (string.IsNullOrEmpty(challengeHeader))
        {
            return null;
        }

        // Format: "<challenge>";id="<sessionId>"
        var value = challengeHeader.TrimStart();
        if (value.StartsWith('"'))
        {
            var end = value.IndexOf('"', 1);
            if (end > 1)
            {
                value = value[1..end];
            }
        }
        else
        {
            var semi = value.IndexOf(';');
            if (semi >= 0)
            {
                value = value[..semi];
            }
        }
        return DecodeChallenge(context, value.Trim());
    }

    public static DebugChallenge DecodeChallenge(HttpContext context, string challenge)
    {
        var dp = context.RequestServices.GetRequiredService<IDataProtectionProvider>();

        byte[] raw;
        try
        {
            raw = WebEncoders.Base64UrlDecode(challenge);
        }
        catch (Exception ex)
        {
            return new DebugChallenge { Valid = false, Error = DescribeException(ex) };
        }

        // The challenge is protected under one of two domain-separated purposes. Try each: the one
        // that decrypts both validates the payload and identifies which flow issued it.
        Exception? firstError = null;
        foreach (var (kind, purpose) in new[]
        {
            ("refresh", DbscNames.RefreshChallengePurpose),
            ("registration", DbscNames.RegistrationChallengePurpose),
        })
        {
            try
            {
                var protector = dp.CreateProtector(purpose).ToTimeLimitedDataProtector();
                var bytes = protector.Unprotect(raw);

                var reader = new CborReader(bytes, allowMultipleRootLevelValues: true);
                var claimUid = reader.ReadTextString();
                string? sessionId = null;
                if (reader.PeekState() != CborReaderState.Finished)
                {
                    sessionId = reader.ReadTextString();
                }

                return new DebugChallenge
                {
                    Kind = kind,
                    ClaimUid = claimUid,
                    SessionId = sessionId,
                    Valid = true,
                };
            }
            catch (Exception ex)
            {
                // Keep the first failure: when a challenge is the correct kind but expired, the
                // matching purpose surfaces an informative "payload expired" message, whereas the
                // other purpose only reports a generic key mismatch.
                firstError ??= ex;
            }
        }

        return new DebugChallenge { Valid = false, Error = DescribeException(firstError!) };
    }

    /// <summary>
    /// Builds a human-readable explanation for a decode failure. For the time-limited
    /// data protector an expired payload surfaces as a CryptographicException whose
    /// message says the payload expired; a key mismatch or tampering surfaces differently.
    /// We include the type, message, and inner-exception chain so the dashboard can show why.
    /// </summary>
    private static string DescribeException(Exception ex)
    {
        var sb = new StringBuilder();
        var current = ex;
        while (current is not null)
        {
            if (sb.Length > 0)
            {
                sb.Append(" -> ");
            }
            sb.Append(current.GetType().Name).Append(": ").Append(current.Message);
            current = current.InnerException;
        }
        return sb.ToString();
    }

    private static DecodedTicket? TryDecodeTicket(HttpContext context, string scheme, string value)
    {
        try
        {
            var monitor = context.RequestServices.GetService<IOptionsMonitor<CookieAuthenticationOptions>>();
            var ticket = monitor?.Get(scheme).TicketDataFormat?.Unprotect(value);
            if (ticket is null)
            {
                return null;
            }

            return new DecodedTicket
            {
                Principal = ticket.Principal.Identity?.Name,
                Claims = ticket.Principal.Claims.Select(c => $"{c.Type} = {c.Value}").ToList(),
                Items = ticket.Properties.Items
                    .Where(kv => kv.Value is not null)
                    .ToDictionary(kv => kv.Key, kv => kv.Value!),
                IssuedUtc = ticket.Properties.IssuedUtc?.ToString("O"),
                ExpiresUtc = ticket.Properties.ExpiresUtc?.ToString("O"),
            };
        }
        catch
        {
            return null;
        }
    }

    private static JsonNode? TryParseJsonSegment(string segment)
    {
        try
        {
            return JsonNode.Parse(WebEncoders.Base64UrlDecode(segment));
        }
        catch
        {
            return null;
        }
    }

    private static string? ReadChunkedCookie(IRequestCookieCollection cookies, string name)
    {
        if (!cookies.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }
        if (value.StartsWith("chunks-", StringComparison.Ordinal) && int.TryParse(value.AsSpan(7), out var count))
        {
            var sb = new StringBuilder();
            for (var i = 1; i <= count; i++)
            {
                if (!cookies.TryGetValue($"{name}C{i}", out var chunk) || chunk is null)
                {
                    return value;
                }
                sb.Append(chunk);
            }
            return sb.ToString();
        }
        return value;
    }

    private static string Reassemble(string name, string value, Dictionary<string, string> byName)
    {
        if (!int.TryParse(value.AsSpan(7), out var count))
        {
            return value;
        }
        var sb = new StringBuilder();
        for (var i = 1; i <= count; i++)
        {
            if (!byName.TryGetValue($"{name}C{i}", out var chunk))
            {
                return value;
            }
            sb.Append(chunk);
        }
        return sb.ToString();
    }

    private static bool IsChunkPart(string name)
    {
        var c = name.LastIndexOf('C');
        return c > 0 && c < name.Length - 1 && int.TryParse(name.AsSpan(c + 1), out _);
    }
}

/// <summary>
/// Records every HTTP exchange (decoded) into <see cref="DbscDebugState"/> for the live dashboard log.
/// </summary>
public sealed class DebugCaptureMiddleware
{
    private readonly RequestDelegate _next;
    private readonly DbscDebugState _state;

    public DebugCaptureMiddleware(RequestDelegate next, DbscDebugState state)
    {
        _next = next;
        _state = state;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var start = DateTimeOffset.UtcNow;
        var requestCookies = DbscDecoder.DecodeRequestCookies(context);
        var proof = DbscDecoder.DecodeProof(context);

        var path = context.Request.Path.Value ?? "";

        // For the DBSC endpoints, buffer the response body so we can capture and decode the
        // JSON Session Instruction the server returns on a 200. Other paths stream as usual.
        var captureBody = IsDbscEndpoint(path);
        Stream? originalBody = null;
        MemoryStream? buffer = null;
        if (captureBody)
        {
            originalBody = context.Response.Body;
            buffer = new MemoryStream();
            context.Response.Body = buffer;
        }

        string? responseBody = null;
        try
        {
            await _next(context);
        }
        finally
        {
            if (captureBody && buffer is not null && originalBody is not null)
            {
                if (context.Response.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true && buffer.Length > 0)
                {
                    responseBody = Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
                }
                buffer.Position = 0;
                await buffer.CopyToAsync(originalBody);
                context.Response.Body = originalBody;
            }

            // Don't log the dashboard's own polling endpoints — it would be infinite noise.
            if (!path.StartsWith("/debug/", StringComparison.Ordinal))
            {
                var registrationHeader = context.Response.Headers["Secure-Session-Registration"].ToString();
                var challengeHeader = context.Response.Headers["Secure-Session-Challenge"].ToString();

                var setCookies = DbscDecoder.DecodeSetCookies(context);

                // The refresh cookie is path-scoped to /.well-known/dbsc, so the dashboard's own
                // /debug/state polls never carry it. Stash the latest decoded copy seen on real DBSC
                // traffic (a fresh Set-Cookie from registration or a slide wins; a deletion clears it).
                UpdateRefreshCookieStash(requestCookies, setCookies);

                var exchange = new DebugExchange
                {
                    Time = start.ToLocalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
                    Method = context.Request.Method,
                    Path = path,
                    Status = context.Response.StatusCode,
                    Category = Categorize(path),
                    Authenticated = context.User.Identity?.IsAuthenticated == true,
                    DurationMs = Math.Round((DateTimeOffset.UtcNow - start).TotalMilliseconds, 2),
                    RequestCookies = requestCookies,
                    SetCookies = setCookies,
                    Proof = proof,
                    RegistrationHeader = string.IsNullOrEmpty(registrationHeader) ? null : registrationHeader,
                    ChallengeHeader = string.IsNullOrEmpty(challengeHeader) ? null : challengeHeader,
                    DecodedChallenge = DbscDecoder.DecodeChallengeHeader(context, challengeHeader),
                    SessionConfig = DbscDecoder.TryParseJsonBody(responseBody),
                };

                _state.Add(exchange);
            }
        }
    }

    private void UpdateRefreshCookieStash(List<DebugCookie>? requestCookies, List<DebugCookie>? setCookies)
    {
        // A fresh Set-Cookie (registration or sliding renewal) reflects the newest expiry; prefer it.
        if (setCookies is not null)
        {
            foreach (var c in setCookies)
            {
                if (c.Name == DbscNames.RefreshCookie)
                {
                    _state.LatestRefreshCookie = c.Deleted ? null : c;
                    return;
                }
            }
        }

        // Otherwise fall back to the refresh cookie the browser sent (e.g. the first leg of a refresh).
        if (requestCookies is not null)
        {
            foreach (var c in requestCookies)
            {
                if (c.Name == DbscNames.RefreshCookie)
                {
                    _state.LatestRefreshCookie = c;
                    return;
                }
            }
        }
    }

    private static bool IsDbscEndpoint(string path) =>
        path is "/.well-known/dbsc/registration" or "/.well-known/dbsc/refresh";

    private static string Categorize(string path) => path switch
    {
        "/.well-known/dbsc/registration" => "register",
        "/.well-known/dbsc/refresh" => "refresh",
        "/login" or "/signout" or "/clear" => "auth",
        _ when path.StartsWith("/api/", StringComparison.Ordinal) => "api",
        _ => "page",
    };
}
