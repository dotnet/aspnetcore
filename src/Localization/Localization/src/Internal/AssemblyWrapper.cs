// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;

namespace Microsoft.Extensions.Localization.Internal
{
    public class AssemblyWrapper
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

        public virtual string FullName => Assembly.FullName;

        public virtual Stream GetManifestResourceStream(string name) => Assembly.GetManifestResourceStream(name);
    }
}
