// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Reference assemblies should have the ReferenceAssemblyAttribute. 
[assembly:System.Runtime.CompilerServices.ReferenceAssembly]

// Reference assemblies should have the 0x70 flag which prevents them from loading.
// This flag sets AssemblyName.ProcessorArchitecture to None. There is no public API for this.
// Cref https://github.com/dotnet/coreclr/blob/64ca544ecf55490675e72b853e98ebc8cc75a4fe/src/System.Private.CoreLib/src/System/Reflection/AssemblyName.CoreCLR.cs#L74
[assembly:System.Reflection.AssemblyFlags((System.Reflection.AssemblyNameFlags)0x70)]
