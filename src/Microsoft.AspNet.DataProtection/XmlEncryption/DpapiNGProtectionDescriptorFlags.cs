// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.DataProtection.XmlEncryption
{
    // from ncrypt.h and ncryptprotect.h
    [Flags]
    public enum DpapiNGProtectionDescriptorFlags
    {
        None = 0,
        NamedDescriptor = 0x00000001,
        MachineKey = 0x00000020,
    }
}
