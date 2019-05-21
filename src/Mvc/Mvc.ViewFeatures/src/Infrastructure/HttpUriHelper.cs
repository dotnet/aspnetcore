// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal class HttpUriHelper : UriHelperBase
    {
        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            throw new NavigationException(uri);
        }
    }
}
