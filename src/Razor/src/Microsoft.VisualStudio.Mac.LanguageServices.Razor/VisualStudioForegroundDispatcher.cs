// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor
{
    [System.Composition.Shared]
    [Export(typeof(ForegroundDispatcher))]
    internal class VisualStudioForegroundDispatcher : ForegroundDispatcher
    {
        public override TaskScheduler BackgroundScheduler { get; } = TaskScheduler.Default;

        public override TaskScheduler ForegroundScheduler { get; } = MonoDevelop.Core.Runtime.MainTaskScheduler;

        public override bool IsForegroundThread => MonoDevelop.Core.Runtime.IsMainThread;
    }
}
