// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.References
{
    internal class TestAssemblyReference : IAssemblyReference
    {
        public AssemblyName AssemblyName { get; set; }

        public string FullPath { get; set; }

        public IProjectProperties Metadata => throw new System.NotImplementedException();
        
        public Task<AssemblyName> GetAssemblyNameAsync()
        {
            return Task.FromResult(AssemblyName);
        }

        public Task<bool> GetCopyLocalAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> GetCopyLocalSatelliteAssembliesAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<string> GetDescriptionAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<string> GetFullPathAsync()
        {
            return Task.FromResult(FullPath);
        }

        public Task<string> GetNameAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> GetReferenceOutputAssemblyAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<string> GetRequiredTargetFrameworkAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> GetSpecificVersionAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> IsWinMDFileAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}
