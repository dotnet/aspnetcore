// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    /// <summary>
    /// Wraps <see cref="RazorEngine"/>, configuring it to compile Blazor components.
    /// </summary>
    public class BlazorRazorEngine
    {
        private readonly RazorEngine _engine;
        private readonly RazorCodeGenerationOptions _codegenOptions;

        public RazorEngine Engine => _engine;

        public BlazorRazorEngine()
        {
            _codegenOptions = RazorCodeGenerationOptions.CreateDefault();

            _engine = RazorEngine.Create(configure =>
            {
                FunctionsDirective.Register(configure);
                InheritsDirective.Register(configure);
                TemporaryLayoutPass.Register(configure);
                TemporaryImplementsPass.Register(configure);

                configure.SetBaseType(BlazorComponent.FullTypeName);

                configure.Phases.Remove(
                    configure.Phases.OfType<IRazorCSharpLoweringPhase>().Single());
                configure.Phases.Add(new BlazorLoweringPhase(_codegenOptions));

                configure.ConfigureClass((codeDoc, classNode) =>
                {
                    configure.SetNamespace((string)codeDoc.Items[BlazorCodeDocItems.Namespace]);
                    classNode.ClassName = (string)codeDoc.Items[BlazorCodeDocItems.ClassName];
                });
            });
        }
    }
}
