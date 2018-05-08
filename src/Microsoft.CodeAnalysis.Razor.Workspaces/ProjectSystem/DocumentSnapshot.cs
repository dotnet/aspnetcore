// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal abstract class DocumentSnapshot
    {
        public abstract string FilePath { get; }

        public abstract string TargetPath { get; }

        public abstract Task<RazorCodeDocument> GetGeneratedOutputAsync();

        public abstract bool TryGetGeneratedOutput(out RazorCodeDocument result);
    }
}
