// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Used to indicate that a something is considered personal data and should be protected.
    /// </summary>
    public class ProtectedPersonalDataAttribute : PersonalDataAttribute
    { }
}