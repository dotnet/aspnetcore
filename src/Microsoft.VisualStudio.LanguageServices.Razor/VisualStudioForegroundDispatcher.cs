// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    [Shared]
    [ExportWorkspaceService(typeof(ForegroundDispatcher), ServiceLayer.Host)]
    internal class VisualStudioForegroundDispatcher : ForegroundDispatcher
    {
        public override bool IsForegroundThread => ThreadHelper.CheckAccess();
    }
}
