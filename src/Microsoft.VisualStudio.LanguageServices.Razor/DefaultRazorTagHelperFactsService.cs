// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    [Export(typeof(RazorTagHelperFactsService))]
    internal class DefaultRazorTagHelperFactsService : RazorTagHelperFactsService
    {
    }
}
