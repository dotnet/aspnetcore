// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.UrlActions;

internal sealed class ChangeCookieAction : UrlAction
{
    private readonly Func<DateTimeOffset> _timeSource;
    private CookieOptions? _cachedOptions;

    public ChangeCookieAction(string name)
        : this(name, () => DateTimeOffset.UtcNow)
    {
    }

    // for testing
    internal ChangeCookieAction(string name, Func<DateTimeOffset> timeSource)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        _timeSource = timeSource;
        Name = name;
    }

    public string Name { get; }
    public string? Value { get; set; }
    public string? Domain { get; set; }
    public TimeSpan Lifetime { get; set; }
    public string? Path { get; set; }
    public bool Secure { get; set; }
    public bool HttpOnly { get; set; }

    public override void ApplyAction(RewriteContext context, BackReferenceCollection? ruleBackReferences, BackReferenceCollection? conditionBackReferences)
    {
        var options = GetOrCreateOptions();
        context.HttpContext.Response.Cookies.Append(Name, Value ?? string.Empty, options);
    }

    private CookieOptions GetOrCreateOptions()
    {
        if (Lifetime > TimeSpan.Zero)
        {
            var now = _timeSource();
            return new CookieOptions()
            {
                Domain = Domain,
                HttpOnly = HttpOnly,
                Secure = Secure,
                Path = Path,
                Expires = now.Add(Lifetime)
            };
        }

        if (_cachedOptions == null)
        {
            _cachedOptions = new CookieOptions()
            {
                Domain = Domain,
                HttpOnly = HttpOnly,
                Secure = Secure,
                Path = Path
            };
        }

        return _cachedOptions;
    }
}
