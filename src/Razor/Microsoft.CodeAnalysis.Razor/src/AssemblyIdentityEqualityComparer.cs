// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Internal;

namespace Microsoft.CodeAnalysis.Razor
{
    internal abstract class AssemblyIdentityEqualityComparer : IEqualityComparer<AssemblyIdentity>
    {
        public static readonly AssemblyIdentityEqualityComparer NameAndVersion = new NameAndVersionEqualityComparer();

        public abstract bool Equals(AssemblyIdentity x, AssemblyIdentity y);

        public abstract int GetHashCode(AssemblyIdentity obj);

        private class NameAndVersionEqualityComparer : AssemblyIdentityEqualityComparer
        {
            public override bool Equals(AssemblyIdentity x, AssemblyIdentity y)
            {
                if (object.ReferenceEquals(x, y))
                {
                    return true;
                }
                else if (x == null ^ y == null)
                {
                    return false;
                }
                else
                {
                    return string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase) && object.Equals(x.Version, y.Version);
                }
            }

            public override int GetHashCode(AssemblyIdentity obj)
            {
                if (obj == null)
                {
                    return 0;
                }

                var hash = new HashCodeCombiner();
                hash.Add(obj.Name, StringComparer.OrdinalIgnoreCase);
                hash.Add(obj.Version);
                return hash;
            }
        }
    }
}
