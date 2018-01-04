// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Diagnostics.Identity.Service
{
    public class DeveloperCertificateViewModel
    {
        public DeveloperCertificateOptions Options { get; set; }
        public bool CertificateExists { get; set; }
        public bool CertificateIsInvalid { get; set; }
        public bool CertificateIsFoundInConfiguration { get; set; }
    }
}
