// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public class IISOptions
    {
        /// <summary>
        /// If true authentication middleware will try to authenticate using platform handler windows authentication
        /// If false authentication middleware won't be added
        /// </summary>
        public bool ForwardWindowsAuthentication { get; set; } = true;

        /// <summary>
        /// Populates the ITLSConnectionFeature if the MS-ASPNETCORE-CLIENTCERT request header is present.
        /// </summary>
        public bool ForwardClientCertificate { get; set; } = true;
    }
}