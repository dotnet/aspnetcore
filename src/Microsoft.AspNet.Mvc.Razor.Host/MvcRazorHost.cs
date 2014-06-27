// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MvcRazorHost : RazorEngineHost, IMvcRazorHost
    {
        private const string ViewNamespace = "ASP";

        private static readonly string[] _defaultNamespaces = new[] 
        { 
            "System",
            "System.Linq",
            "System.Collections.Generic",
            "Microsoft.AspNet.Mvc",
            "Microsoft.AspNet.Mvc.Razor",
            "Microsoft.AspNet.Mvc.Rendering"
        };

        private readonly MvcRazorHostOptions _hostOptions;

        // CodeGenerationContext.DefaultBaseClass is set to MyBaseType<dynamic>. 
        // This field holds the type name without the generic decoration (MyBaseType)
        private readonly string _baseType;

        public MvcRazorHost(Type baseType)
            : this(baseType.FullName)
        {
        }

        public MvcRazorHost(string baseType)
            : base(new CSharpRazorCodeLanguage())
        {
            // TODO: this needs to flow from the application rather than being initialized here.
            // Tracked by #774
            _hostOptions = new MvcRazorHostOptions();
            _baseType = baseType;
            DefaultBaseClass = baseType + '<' + _hostOptions.DefaultModel + '>';
            GeneratedClassContext = new GeneratedClassContext(
                executeMethodName: "ExecuteAsync",
                writeMethodName: "Write",
                writeLiteralMethodName: "WriteLiteral",
                writeToMethodName: "WriteTo",
                writeLiteralToMethodName: "WriteLiteralTo",
                templateTypeName: "HelperResult",
                defineSectionMethodName: "DefineSection")
            {
                ResolveUrlMethodName = "Href"
            };

            foreach (var ns in _defaultNamespaces)
            {
                NamespaceImports.Add(ns);
            }
        }

        public GeneratorResults GenerateCode(string rootRelativePath, Stream inputStream)
        {
            var className = ParserHelpers.SanitizeClassName(rootRelativePath);
            using (var reader = new StreamReader(inputStream))
            {
                var engine = new RazorTemplateEngine(this);
                return engine.GenerateCode(reader, className, ViewNamespace, rootRelativePath);
            }
        }

        public override ParserBase DecorateCodeParser(ParserBase incomingCodeParser)
        {
            return new MvcRazorCodeParser(_baseType);
        }

        public override CodeBuilder DecorateCodeBuilder(CodeBuilder incomingBuilder, CodeGeneratorContext context)
        {
            UpdateCodeBuilder(context);
            return new MvcCSharpCodeBuilder(context, _hostOptions);
        }

        private void UpdateCodeBuilder(CodeGeneratorContext context)
        {
            var currentChunks = context.CodeTreeBuilder.CodeTree.Chunks;
            var existingInjects = new HashSet<string>(currentChunks.OfType<InjectChunk>()
                                                                   .Select(c => c.MemberName),
                                                      StringComparer.Ordinal);

            var modelChunk = currentChunks.OfType<ModelChunk>()
                                          .LastOrDefault();
            var model = _hostOptions.DefaultModel;
            if (modelChunk != null)
            {
                model = modelChunk.ModelType;
            }
            model = '<' + model + '>';

            // Locate properties by name that haven't already been injected in to the View.
            var propertiesToAdd = _hostOptions.DefaultInjectedProperties
                                              .Where(c => !existingInjects.Contains(c.MemberName));
            foreach (var property in propertiesToAdd)
            {
                var typeName = property.TypeName.Replace("<TModel>", model);
                currentChunks.Add(new InjectChunk(typeName, property.MemberName));
            }
        }
    }
}
