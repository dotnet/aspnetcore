// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Runtime.Serialization;
using ResourceHashesByNameDictionary = System.Collections.Generic.Dictionary<string, string>;

namespace Microsoft.AspNetCore.Razor.Tasks
{
#pragma warning disable IDE1006 // Naming Styles
    /// <summary>
    /// Defines the structure of a Blazor boot JSON file
    /// </summary>
    public class BootJsonData
    {
        /// <summary>
        /// Gets the name of the assembly with the application entry point
        /// </summary>
        public string entryAssembly { get; set; }

        /// <summary>
        /// Gets the set of resources needed to boot the application. This includes the transitive
        /// closure of .NET assemblies (including the entrypoint assembly), the dotnet.wasm file,
        /// and any PDBs to be loaded.
        ///
        /// Within <see cref="ResourceHashesByNameDictionary"/>, dictionary keys are resource names,
        /// and values are SHA-256 hashes formatted in prefixed base-64 style (e.g., 'sha256-abcdefg...')
        /// as used for subresource integrity checking.
        /// </summary>
        public ResourcesData resources { get; set; } = new ResourcesData();

        /// <summary>
        /// Gets a value that determines whether to enable caching of the <see cref="resources"/>
        /// inside a CacheStorage instance within the browser.
        /// </summary>
        public bool cacheBootResources { get; set; }

        /// <summary>
        /// Gets a value that determines if this is a debug build.
        /// </summary>
        public bool debugBuild { get; set; }

        /// <summary>
        /// Gets a value that determines if the linker is enabled.
        /// </summary>
        public bool linkerEnabled { get; set; }

        /// <summary>
        /// Config files for the application
        /// </summary>
        public List<string> config { get; set; }
    }

    public class ResourcesData
    {
        /// <summary>
        /// .NET Wasm runtime resources (dotnet.wasm, dotnet.js) etc.
        /// </summary>
        public ResourceHashesByNameDictionary runtime { get; set; } = new ResourceHashesByNameDictionary();

        /// <summary>
        /// "assembly" (.dll) resources
        /// </summary>
        public ResourceHashesByNameDictionary assembly { get; set; } = new ResourceHashesByNameDictionary();

        /// <summary>
        /// "debug" (.pdb) resources
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public ResourceHashesByNameDictionary pdb { get; set; }

        /// <summary>
        /// localization (.satellite resx) resources
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, ResourceHashesByNameDictionary> satelliteResources { get; set; }
    }
#pragma warning restore IDE1006 // Naming Styles
}
