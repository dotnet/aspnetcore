// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Razor
{
    // Provides access to Razor language and workspace services that are avialable in the OOP host.
    //
    // Since we don't have access to the workspace we only have access to some specific things
    // that we can construct directly.
    internal class RazorServices
    {
        public RazorServices()
        {
            FallbackProjectEngineFactory = new FallbackProjectEngineFactory();
            TagHelperResolver = new RemoteTagHelperResolver(FallbackProjectEngineFactory);
        }

        public IFallbackProjectEngineFactory FallbackProjectEngineFactory { get; }

        public RemoteTagHelperResolver TagHelperResolver { get; }
    }
}
