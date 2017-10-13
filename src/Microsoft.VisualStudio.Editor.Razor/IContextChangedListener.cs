// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Editor.Razor
{
    /// <summary>
    /// This class will cease to be useful once the Razor tooling owns TagHelper discovery
    /// </summary>
    public interface IContextChangedListener
    {
        void OnContextChanged(VisualStudioRazorParser parser);
    }
}
