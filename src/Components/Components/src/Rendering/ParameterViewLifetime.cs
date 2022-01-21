// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Rendering;

internal readonly struct ParameterViewLifetime
{
    private readonly RenderBatchBuilder _owner;
    private readonly int _stamp;

    public static readonly ParameterViewLifetime Unbound;

    public ParameterViewLifetime(RenderBatchBuilder owner)
    {
        _owner = owner;
        _stamp = owner.ParameterViewValidityStamp;
    }

    public void AssertNotExpired()
    {
        // If _owner is null, this instance is default(ParameterViewLifetime), which is
        // the same as ParameterViewLifetime.Unbound. That means it never expires.
        if (_owner != null && _owner.ParameterViewValidityStamp != _stamp)
        {
            throw new InvalidOperationException($"The {nameof(ParameterView)} instance can no longer be read because it has expired. {nameof(ParameterView)} can only be read synchronously and must not be stored for later use.");
        }
    }
}
