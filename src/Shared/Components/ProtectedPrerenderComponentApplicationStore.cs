// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.AspNetCore.Components;

internal sealed class ProtectedPrerenderComponentApplicationStore : PrerenderComponentApplicationStore
{
    private IDataProtector _protector;

    public ProtectedPrerenderComponentApplicationStore(IDataProtectionProvider dataProtectionProvider) : base()
    {
        CreateProtector(dataProtectionProvider);
    }

    public ProtectedPrerenderComponentApplicationStore(string existingState, IDataProtectionProvider dataProtectionProvider)
    {
        CreateProtector(dataProtectionProvider);
        DeserializeState(_protector.Unprotect(Convert.FromBase64String(existingState)));
    }

    protected override byte[] SerializeState(IReadOnlyDictionary<string, byte[]> state)
    {
        var bytes = base.SerializeState(state);
        return _protector != null ? _protector.Protect(bytes) : bytes;
    }

    private void CreateProtector(IDataProtectionProvider dataProtectionProvider) =>
        _protector = dataProtectionProvider.CreateProtector("Microsoft.AspNetCore.Components.Server.State");
}
