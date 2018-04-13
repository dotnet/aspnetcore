// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Blazor.Components
{
    /// <summary>
    /// Holds <see cref="EventHandler"/> attributes to configure the mappings between event names and
    /// event argument types.
    /// </summary>
    [EventHandler("onchange", typeof(UIChangeEventArgs))]
    [EventHandler("onclick", typeof(UIMouseEventArgs))]
    public static class EventHandlers
    {
    }
}
