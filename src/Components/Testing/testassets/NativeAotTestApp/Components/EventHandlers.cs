// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using NativeAotTestApp.Models;

namespace NativeAotTestApp.Components;

[EventHandler("onsplitprobe", typeof(SplitEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
public static class EventHandlers;
