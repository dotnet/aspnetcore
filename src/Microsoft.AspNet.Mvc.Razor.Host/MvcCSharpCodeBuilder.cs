// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Generator.Compiler.CSharp;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MvcCSharpCodeBuilder : CSharpCodeBuilder
    {
        private readonly MvcRazorHostOptions _hostOptions;

        public MvcCSharpCodeBuilder([NotNull] CodeGeneratorContext context, 
                                    [NotNull] MvcRazorHostOptions hostOptions)
            : base(context)
        {
            _hostOptions = hostOptions;
        }

        private string Model { get; set; }

        protected override CSharpCodeWritingScope BuildClassDeclaration(CSharpCodeWriter writer)
        {
            // Grab the last model chunk so it gets intellisense.
            // NOTE: If there's more than 1 model chunk there will be a Razor error BUT we want intellisense to 
            // show up on the current model chunk that the user is typing.
            var modelChunk = Context.CodeTreeBuilder.CodeTree.Chunks.OfType<ModelChunk>()
                                                                    .LastOrDefault();

            Model = modelChunk != null ? modelChunk.ModelType : _hostOptions.DefaultModel;

            // If there were any model chunks then we need to modify the class declaration signature.
            if (modelChunk != null)
            {
                writer.Write(string.Format(CultureInfo.InvariantCulture, "public class {0} : ", Context.ClassName));

                var modelVisitor = new ModelChunkVisitor(writer, Context);
                // This generates the base class signature
                modelVisitor.Accept(modelChunk);

                writer.WriteLine();

                return new CSharpCodeWritingScope(writer);
            }
            else
            {
                return base.BuildClassDeclaration(writer);
            }
        }

        protected override void BuildConstructor([NotNull] CSharpCodeWriter writer)
        {
            // TODO: Move this to a proper extension point. Right now, we don't have a place to print out properties
            // in the generated view.
            // Tracked by #773
            base.BuildConstructor(writer);

            writer.WriteLineHiddenDirective();

            var injectVisitor = new InjectChunkVisitor(writer, Context, _hostOptions.ActivateAttributeName);
            injectVisitor.Accept(Context.CodeTreeBuilder.CodeTree.Chunks);

            writer.WriteLine();
            writer.WriteLineHiddenDirective();
        }
    }
}