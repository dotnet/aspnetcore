// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite;

// For more information of flags, and what flags we currently support:
// https://github.com/aspnet/BasicMiddleware/issues/66
// http://httpd.apache.org/docs/current/expr.html#vars
internal sealed class Flags
{
    public IDictionary<FlagType, string> FlagDictionary { get; }

    public Flags(IDictionary<FlagType, string> flags)
    {
        FlagDictionary = flags;
    }

    public Flags()
    {
        FlagDictionary = new Dictionary<FlagType, string>();
    }

    public void SetFlag(FlagType flag, string value)
    {
        if (value == null)
        {
            value = string.Empty;
        }
        FlagDictionary[flag] = value;
    }

    public bool GetValue(FlagType flag, [NotNullWhen(true)] out string? value)
    {
        if (!FlagDictionary.TryGetValue(flag, out var res))
        {
            value = null;
            return false;
        }
        value = res;
        return true;
    }

    public string? this[FlagType flag]
    {
        get
        {
            if (!FlagDictionary.TryGetValue(flag, out var res))
            {
                return null;
            }
            return res;
        }
        set
        {
            FlagDictionary[flag] = value ?? string.Empty;
        }
    }

    public bool HasFlag(FlagType flag)
    {
        return FlagDictionary.ContainsKey(flag);
    }
}
