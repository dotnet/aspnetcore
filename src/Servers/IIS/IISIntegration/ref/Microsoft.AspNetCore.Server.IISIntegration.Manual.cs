// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public partial class IISOptions
    {
        internal bool ForwardWindowsAuthentication { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal partial class IISSetupFilter : Microsoft.AspNetCore.Hosting.IStartupFilter
    {
        internal IISSetupFilter(string pairingToken, Microsoft.AspNetCore.Http.PathString pathBase, bool isWebsocketsSupported) { }
        public System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> Configure(System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> next) { throw null; }
    }
}
