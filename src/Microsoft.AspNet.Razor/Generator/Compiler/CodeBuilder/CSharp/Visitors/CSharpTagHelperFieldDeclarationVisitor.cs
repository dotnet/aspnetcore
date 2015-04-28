// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpTagHelperFieldDeclarationVisitor : CodeVisitor<CSharpCodeWriter>
    {
        private readonly HashSet<string> _declaredTagHelpers;
        private readonly GeneratedTagHelperContext _tagHelperContext;
        private bool _foundTagHelpers;

        public CSharpTagHelperFieldDeclarationVisitor([NotNull] CSharpCodeWriter writer,
                                                      [NotNull] CodeBuilderContext context)
            : base(writer, context)
        {
            _declaredTagHelpers = new HashSet<string>(StringComparer.Ordinal);
            _tagHelperContext = Context.Host.GeneratedClassContext.GeneratedTagHelperContext;
        }

        protected override void Visit(TagHelperChunk chunk)
        {
            // We only want to setup tag helper manager fields if there are tag helpers, and only once
            if (!_foundTagHelpers)
            {
                _foundTagHelpers = true;

                // We want to hide declared TagHelper fields so they cannot be stepped over via a debugger.
                Writer.WriteLineHiddenDirective();

                // Runtime fields aren't useful during design time.
                if (!Context.Host.DesignTimeMode)
                {
                    // Need to disable the warning "X is assigned to but never used." for the value buffer since
                    // whether it's used depends on how a TagHelper is used.
                    Writer.WritePragma("warning disable 0414");
                    WritePrivateField(_tagHelperContext.TagHelperContentTypeName,
                                      CSharpTagHelperCodeRenderer.StringValueBufferVariableName,
                                      value: null);
                    Writer.WritePragma("warning restore 0414");

                    WritePrivateField(_tagHelperContext.ExecutionContextTypeName,
                                      CSharpTagHelperCodeRenderer.ExecutionContextVariableName,
                                      value: null);

                    Writer
                        .Write("private ")
                        .WriteVariableDeclaration(
                        _tagHelperContext.RunnerTypeName,
                        CSharpTagHelperCodeRenderer.RunnerVariableName,
                        value: null);

                    Writer.Write("private ")
                          .Write(_tagHelperContext.ScopeManagerTypeName)
                          .Write(" ")
                          .WriteStartAssignment(CSharpTagHelperCodeRenderer.ScopeManagerVariableName)
                          .WriteStartNewObject(_tagHelperContext.ScopeManagerTypeName)
                          .WriteEndMethodInvocation();
                }
            }

            foreach (var descriptor in chunk.Descriptors)
            {
                if (!_declaredTagHelpers.Contains(descriptor.TypeName))
                {
                    _declaredTagHelpers.Add(descriptor.TypeName);

                    WritePrivateField(descriptor.TypeName,
                                      CSharpTagHelperCodeRenderer.GetVariableName(descriptor),
                                      value: null);
                }
            }

            // We need to dive deeper to ensure we pick up any nested tag helpers.
            Accept(chunk.Children);
        }

        public override void Accept(Chunk chunk)
        {
            var chunkBlock = chunk as ChunkBlock;

            // If we're any ChunkBlock other than TagHelperChunk then we want to dive into its Children
            // to search for more TagHelperChunk chunks. This if-statement enables us to not override
            // each of the special ChunkBlock types and then dive into their children.
            if (chunkBlock != null && !(chunkBlock is TagHelperChunk))
            {
                Accept(chunkBlock.Children);
            }
            else
            {
                // If we're a TagHelperChunk or any other non ChunkBlock we ".Accept" it. This ensures
                // that our overriden Visit(TagHelperChunk) method gets called and is not skipped over.
                // If we're a non ChunkBlock or a TagHelperChunk then we want to just invoke the Visit
                // method for that given chunk (base.Accept indirectly calls the Visit method).
                base.Accept(chunk);
            }
        }

        private void WritePrivateField(string type, string name, string value)
        {
            Writer.Write("private ")
                  .WriteVariableDeclaration(type, name, value);
        }
    }
}