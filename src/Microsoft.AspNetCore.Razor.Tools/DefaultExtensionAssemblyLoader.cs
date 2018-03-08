// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal class DefaultExtensionAssemblyLoader : ExtensionAssemblyLoader
    {
        private readonly string _baseDirectory;

        private readonly object _lock = new object();
        private readonly Dictionary<string, (Assembly assembly, AssemblyIdentity identity)> _loadedByPath;
        private readonly Dictionary<AssemblyIdentity, Assembly> _loadedByIdentity;
        private readonly Dictionary<string, AssemblyIdentity> _identityCache;
        private readonly Dictionary<string, List<string>> _wellKnownAssemblies;

        private ShadowCopyManager _shadowCopyManager;

        public DefaultExtensionAssemblyLoader(string baseDirectory)
        {
            _baseDirectory = baseDirectory;

            _loadedByPath = new Dictionary<string, (Assembly assembly, AssemblyIdentity identity)>(StringComparer.OrdinalIgnoreCase);
            _loadedByIdentity = new Dictionary<AssemblyIdentity, Assembly>();
            _identityCache = new Dictionary<string, AssemblyIdentity>(StringComparer.OrdinalIgnoreCase);
            _wellKnownAssemblies = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            LoadContext = new ExtensionAssemblyLoadContext(AssemblyLoadContext.GetLoadContext(typeof(ExtensionAssemblyLoader).Assembly), this);
        }

        protected AssemblyLoadContext LoadContext { get; }

        public override void AddAssemblyLocation(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (!Path.IsPathRooted(filePath))
            {
                throw new ArgumentException(nameof(filePath));
            }

            var assemblyName = Path.GetFileNameWithoutExtension(filePath);
            lock (_lock)
            {
                if (!_wellKnownAssemblies.TryGetValue(assemblyName, out var paths))
                {
                    paths = new List<string>();
                    _wellKnownAssemblies.Add(assemblyName, paths);
                }

                if (!paths.Contains(filePath))
                {
                    paths.Add(filePath);
                }
            }
        }

        public override Assembly Load(string assemblyName)
        {
            if (!AssemblyIdentity.TryParseDisplayName(assemblyName, out var identity))
            {
                return null;
            }

            lock (_lock)
            {
                // First, check if this loader already loaded the requested assembly:
                if (_loadedByIdentity.TryGetValue(identity, out var assembly))
                {
                    return assembly;
                }

                // Second, check if an assembly file of the same simple name was registered with the loader:
                if (_wellKnownAssemblies.TryGetValue(identity.Name, out var paths))
                {
                    // Multiple assemblies of the same simple name but different identities might have been registered.
                    // Load the one that matches the requested identity (if any).
                    foreach (var path in paths)
                    {
                        var candidateIdentity = GetIdentity(path);

                        if (identity.Equals(candidateIdentity))
                        {
                            return LoadFromPathUnsafe(path, candidateIdentity);
                        }
                    }
                }

                // We only support loading by name from 'well-known' paths. If you need to load something by
                // name and you get here, then that means we don't know where to look.
                return null;
            }
        }

        public override Assembly LoadFromPath(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (!Path.IsPathRooted(filePath))
            {
                throw new ArgumentException(nameof(filePath));
            }

            lock (_lock)
            {
                return LoadFromPathUnsafe(filePath, identity: null);
            }
        }

        private Assembly LoadFromPathUnsafe(string filePath, AssemblyIdentity identity)
        {
            // If we've already loaded the assembly by path there should be nothing else to do,
            // all of our data is up to date.
            if (_loadedByPath.TryGetValue(filePath, out var entry))
            {
                return entry.assembly;
            }

            // If we've already loaded the assembly by identity, then we might has some updating
            // to do.
            identity = identity ?? GetIdentity(filePath);
            if (identity != null && _loadedByIdentity.TryGetValue(identity, out var assembly))
            {
                // An assembly file might be replaced by another file with a different identity.
                // Last one wins.
                _loadedByPath[filePath] = (assembly, identity);
                return assembly;
            }

            // Ok we don't have this cached. Let's actually try to load the assembly.
            assembly = LoadFromPathUnsafeCore(CopyAssembly(filePath));

            identity = identity ?? AssemblyIdentity.FromAssemblyDefinition(assembly);

            // It's possible an assembly was loaded by two different paths. Just use the original then.
            if (_loadedByIdentity.TryGetValue(identity, out var duplicate))
            {
                assembly = duplicate;
            }
            else
            {
                _loadedByIdentity.Add(identity, assembly);
            }

            _loadedByPath[filePath] = (assembly, identity);
            return assembly;
        }

        private AssemblyIdentity GetIdentity(string filePath)
        {
            if (!_identityCache.TryGetValue(filePath, out var identity))
            {
                identity = ReadAssemblyIdentity(filePath);
                _identityCache.Add(filePath, identity);
            }

            return identity;
        }

        protected virtual string CopyAssembly(string filePath)
        {
            if (_baseDirectory == null)
            {
                // Don't shadow-copy when base directory is null. This means we're running as a CLI not
                // a server.
                return filePath;
            }

            if (_shadowCopyManager == null)
            {
                _shadowCopyManager = new ShadowCopyManager(_baseDirectory);
            }

            return _shadowCopyManager.AddAssembly(filePath);
        }

        protected virtual Assembly LoadFromPathUnsafeCore(string filePath)
        {
            return LoadContext.LoadFromAssemblyPath(filePath);
        }

        private static AssemblyIdentity ReadAssemblyIdentity(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new PEReader(stream))
                {
                    var metadataReader = reader.GetMetadataReader();
                    return metadataReader.GetAssemblyIdentity();
                }
            }
            catch
            {
            }

            return null;
        }

        private class ExtensionAssemblyLoadContext : AssemblyLoadContext
        {
            private readonly AssemblyLoadContext _parent;
            private readonly DefaultExtensionAssemblyLoader _loader;

            public ExtensionAssemblyLoadContext(AssemblyLoadContext parent, DefaultExtensionAssemblyLoader loader)
            {
                _parent = parent;
                _loader = loader;
            }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                // Try to load from well-known paths. This will be called when loading a dependency of an extension.
                var assembly = _loader.Load(assemblyName.ToString());
                if (assembly != null)
                {
                    return assembly;
                }

                // If we don't have an entry, then fall back to the default load context. This allows extensions
                // to resolve assemblies that are provided by the host.
                return _parent.LoadFromAssemblyName(assemblyName);
            }
        }
    }
}