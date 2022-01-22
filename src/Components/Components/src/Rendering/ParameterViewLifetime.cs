// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.Rendering
{
    internal readonly struct ParameterViewLifetime
    {
        private readonly RenderBatchBuilder _owner;
        private readonly int _stamp;

        public static readonly ParameterViewLifetime Unbound = default;

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
}
