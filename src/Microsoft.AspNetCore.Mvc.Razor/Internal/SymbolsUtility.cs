// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Mvc.Razor.Internal
{
    /// <summary>
    /// Utility type for determining if a platform supports symbol file generation.
    /// </summary>
    public class SymbolsUtility
    {
        private const string SymWriterGuid = "0AE2DEB0-F901-478b-BB9F-881EE8066788";
        private static readonly Lazy<bool> _isMono = new Lazy<bool>(() => Type.GetType("Mono.Runtime") != null);

        /// <summary>
        /// Determines if the current platform supports symbols (pdb) generation.
        /// </summary>
        /// <returns><c>true</c> if pdb generation is supported; <c>false</c> otherwise.</returns>
        public static bool SupportsSymbolsGeneration()
        {
            if (_isMono.Value)
            {
                return false;
            }

            try
            {
                // Check for the pdb writer component that roslyn uses to generate pdbs
                var type = Marshal.GetTypeFromCLSID(new Guid(SymWriterGuid));
                if (type != null)
                {
                    // This line will throw if pdb generation is not supported.
                    Activator.CreateInstance(type);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }
    }
}