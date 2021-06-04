// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// Default implementation for <see cref="IHttpActivityFeature"/>.
    /// </summary>
    internal sealed class ActivityFeature : IHttpActivityFeature
    {
        internal ActivityFeature(Activity activity)
        {
            Activity = activity;
        }

        /// <inheritdoc />
        public Activity Activity { get; set; }
    }
}
