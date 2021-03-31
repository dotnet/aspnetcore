// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;

namespace Microsoft.Extensions.Localization
{
    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    internal class AssemblyWrapper
    {
        public AssemblyWrapper(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            Assembly = assembly;
        }

        public Assembly Assembly { get; }

        public virtual string FullName => Assembly.FullName!;

        public virtual Stream? GetManifestResourceStream(string name) => Assembly.GetManifestResourceStream(name);
    }
}
