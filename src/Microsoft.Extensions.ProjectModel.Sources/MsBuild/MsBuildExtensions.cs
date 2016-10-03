// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.Build.Execution
{
    internal static class MsBuildExtensions
    {
        public static string FindProperty(this ProjectInstance projectInstance, string propertyName, StringComparison comparer)
            => projectInstance.Properties.FirstOrDefault(p => p.Name.Equals(propertyName, comparer))?.EvaluatedValue;
    }
}
