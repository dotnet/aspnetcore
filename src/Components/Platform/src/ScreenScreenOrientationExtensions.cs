// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Provides access to the Screen Orientation API from a <see cref="Screen"/>.
/// </summary>
public static class ScreenScreenOrientationExtensions
{
    extension(Screen screen)
    {
        /// <summary>
        /// Gets the orientation of the screen.
        /// </summary>
        public ScreenOrientation Orientation
            => screen.GetOrAddFeature(static context => new ScreenOrientation(context.JSRuntime, context.CreateEventListenerCollection()));
    }
}
