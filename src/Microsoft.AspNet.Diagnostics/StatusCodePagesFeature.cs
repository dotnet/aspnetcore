// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Diagnostics
{
    /// <summary>
    /// Represents the Status code pages feature.
    /// </summary>
    public class StatusCodePagesFeature : IStatusCodePagesFeature
    {
        public bool Enabled { get; set; } = true;
    }
}