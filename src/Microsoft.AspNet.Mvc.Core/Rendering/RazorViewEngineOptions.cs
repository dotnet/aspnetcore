// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.Core
{
    /// <summary>
    /// Provides programmatic configuration for the default <see cref="Microsoft.AspNet.Mvc.Rendering.IViewEngine"/>.
    /// </summary>
    public class RazorViewEngineOptions
    {
        private TimeSpan _expirationBeforeCheckingFilesOnDisk = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Controls the <see cref="ExpiringFileInfoCache" /> caching behavior.
        /// </summary>
        /// <remarks>
        /// <see cref="TimeSpan"/> of <see cref="TimeSpan.Zero"/> or less, means no caching.
        /// <see cref="TimeSpan"/> of <see cref="TimeSpan.MaxValue"/> means indefinite caching.
        /// </remarks>
        public TimeSpan ExpirationBeforeCheckingFilesOnDisk
        {
            get
            {
                return _expirationBeforeCheckingFilesOnDisk;
            }

            set
            {
                if (value.TotalMilliseconds < 0)
                {
                    _expirationBeforeCheckingFilesOnDisk = TimeSpan.Zero;
                }
                else
                {
                    _expirationBeforeCheckingFilesOnDisk = value;
                }
            }
        }
    }
}
