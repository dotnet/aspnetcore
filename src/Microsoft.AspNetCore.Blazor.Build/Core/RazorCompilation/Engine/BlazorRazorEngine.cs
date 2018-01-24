// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Blazor.Components;
using System.Linq;

namespace Microsoft.AspNetCore.Blazor.Build.Core.RazorCompilation.Engine
{
    /// <summary>
    /// Wraps <see cref="RazorEngine"/>, configuring it to compile Blazor components.
    /// </summary>
    internal class BlazorRazorEngine
    {
        private readonly RazorEngine _engine;
        private readonly RazorCodeGenerationOptions _codegenOptions;

        public BlazorRazorEngine()
        {
            _codegenOptions = RazorCodeGenerationOptions.CreateDefault();

            _engine = RazorEngine.Create(configure =>
            {
                FunctionsDirective.Register(configure);

                configure.SetBaseType(typeof(BlazorComponent).FullName);

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

        public void Process(RazorCodeDocument document)
            => _engine.Process(document);
    }
}
