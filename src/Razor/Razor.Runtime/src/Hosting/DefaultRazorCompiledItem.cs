// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Hosting;

internal sealed class DefaultRazorCompiledItem : RazorCompiledItem
{
    private object[] _metadata;

    public DefaultRazorCompiledItem(Type type, string kind, string identifier)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(identifier);

        Type = type;
        Kind = kind;
        Identifier = identifier;
    }

    public override string Identifier { get; }

    public override string Kind { get; }

    public override IReadOnlyList<object> Metadata
    {
        get
        {
            if (_metadata == null)
            {
                _metadata = Type.GetCustomAttributes(inherit: true);
            }

            return _metadata;
        }
    }

    public override Type Type { get; }
}
