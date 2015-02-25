// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// we only ever p/invoke into DLLs known to be in the System32 folder
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

[assembly: InternalsVisibleTo("Microsoft.AspNet.Cryptography.Internal.Test")]
[assembly: InternalsVisibleTo("Microsoft.AspNet.Cryptography.KeyDerivation")]
[assembly: InternalsVisibleTo("Microsoft.AspNet.Cryptography.KeyDerivation.Test")]
[assembly: InternalsVisibleTo("Microsoft.AspNet.Security.DataProtection")]
[assembly: InternalsVisibleTo("Microsoft.AspNet.Security.DataProtection.Test")]
