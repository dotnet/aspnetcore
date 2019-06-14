// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// All reference assemblies should have the 0x70 flag which prevents them from loading
// and the ReferenceAssemblyAttribute.
[assembly:System.Runtime.CompilerServices.ReferenceAssembly]
[assembly:System.Reflection.AssemblyFlags((System.Reflection.AssemblyNameFlags)0x70)]
