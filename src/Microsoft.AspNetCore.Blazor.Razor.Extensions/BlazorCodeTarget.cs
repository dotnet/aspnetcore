// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Blazor.Razor
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
