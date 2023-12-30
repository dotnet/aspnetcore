// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.AspNetCore.Components;

internal sealed class ProtectedPrerenderComponentApplicationStore : PrerenderComponentApplicationStore
{
    private IDataProtector _protector = default!; // Assigned in all constructor paths

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

    public override bool SupportsRenderMode(IComponentRenderMode renderMode) =>
        renderMode is null ||
        renderMode is InteractiveServerRenderMode || renderMode is InteractiveAutoRenderMode;
}
