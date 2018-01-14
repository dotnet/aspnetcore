// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using System;

namespace Microsoft.Blazor.Build.Core.RazorCompilation.Engine
{
    /// <summary>
    /// Directs a <see cref="DocumentWriter"/> to use <see cref="BlazorIntermediateNodeWriter"/>.
    /// </summary>
    internal class BlazorCodeTarget : CodeTarget
    {
        public override IntermediateNodeWriter CreateNodeWriter()
            => new BlazorIntermediateNodeWriter();

        public override TExtension GetExtension<TExtension>()
            => throw new NotImplementedException();

        public override bool HasExtension<TExtension>()
            => throw new NotImplementedException();
    }
}
