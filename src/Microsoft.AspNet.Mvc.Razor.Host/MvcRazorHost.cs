// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Mvc.Razor.Directives;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MvcRazorHost : RazorEngineHost, IMvcRazorHost
    {
        private const string BaseType = "Microsoft.AspNet.Mvc.Razor.RazorPage";
        private static readonly string[] _defaultNamespaces = new[]
        {
            "System",
            "System.Linq",
            "System.Collections.Generic",
            "Microsoft.AspNet.Mvc",
            "Microsoft.AspNet.Mvc.Rendering",
        };
        private static readonly Chunk[] _defaultInheritedChunks = new[]
        {
            new InjectChunk("Microsoft.AspNet.Mvc.Rendering.IHtmlHelper<TModel>", "Html"),
            new InjectChunk("Microsoft.AspNet.Mvc.IViewComponentHelper", "Component"),
            new InjectChunk("Microsoft.AspNet.Mvc.IUrlHelper", "Url"),
        };

        private readonly IFileSystem _fileSystem;
        // CodeGenerationContext.DefaultBaseClass is set to MyBaseType<dynamic>. 
        // This field holds the type name without the generic decoration (MyBaseType)
        private readonly string _baseType;

        /// <summary>
        /// Initializes a new instance of <see cref="MvcRazorHost"/> with the specified
        /// <param name="appEnvironment"/>.
        /// </summary>
        /// <param name="appEnvironment">Contains information about the executing application.</param>
        public MvcRazorHost(IApplicationEnvironment appEnvironment)
            : this(new PhysicalFileSystem(appEnvironment.ApplicationBasePath))
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MvcRazorHost"/> using the specified <paramref name="fileSystem"/>.
        /// </summary>
        /// <param name="fileSystem">A <see cref="IFileSystem"/> rooted at the application base path.</param>
        protected internal MvcRazorHost([NotNull] IFileSystem fileSystem)
            : base(new CSharpRazorCodeLanguage())
        {
            _fileSystem = fileSystem;
            _baseType = BaseType;

            DefaultBaseClass = BaseType + '<' + DefaultModel + '>';
            DefaultNamespace = "Asp";
            GeneratedClassContext = new GeneratedClassContext(
                executeMethodName: "ExecuteAsync",
                writeMethodName: "Write",
                writeLiteralMethodName: "WriteLiteral",
                writeToMethodName: "WriteTo",
                writeLiteralToMethodName: "WriteLiteralTo",
                templateTypeName: "Microsoft.AspNet.Mvc.Razor.HelperResult",
                defineSectionMethodName: "DefineSection")
            {
                ResolveUrlMethodName = "Href"
            };

            foreach (var ns in _defaultNamespaces)
            {
                NamespaceImports.Add(ns);
            }
        }

        /// <summary>
        /// Gets the model type used by default when no model is specified.
        /// </summary>
        /// <remarks>This value is used as the generic type argument for the base type </remarks>
        public virtual string DefaultModel
        {
            get { return "dynamic"; }
        }

        /// <summary>
        /// Gets the list of chunks that are injected by default by this host.
        /// </summary>
        public virtual IReadOnlyList<Chunk> DefaultInheritedChunks
        {
            get { return _defaultInheritedChunks; }
        }

        /// <summary>
        /// Gets or sets the name attribute that is used to decorate properties that are injected and need to be
        /// activated.
        /// </summary>
        public virtual string ActivateAttribute
        {
            get { return "Microsoft.AspNet.Mvc.ActivateAttribute"; }
        }

        /// <inheritdoc />
        public GeneratorResults GenerateCode(string rootRelativePath, Stream inputStream)
        {
            var className = ParserHelpers.SanitizeClassName(rootRelativePath);
            using (var reader = new StreamReader(inputStream))
            {
                var engine = new RazorTemplateEngine(this);
                return engine.GenerateCode(reader, className, DefaultNamespace, rootRelativePath);
            }
        }

        /// <inheritdoc />
        public override ParserBase DecorateCodeParser([NotNull] ParserBase incomingCodeParser)
        {
            return new MvcRazorCodeParser(_baseType);
        }

        /// <inheritdoc />
        public override CodeBuilder DecorateCodeBuilder([NotNull] CodeBuilder incomingBuilder,
                                                        [NotNull] CodeGeneratorContext context)
        {
            UpdateCodeBuilder(context);
            return new MvcCSharpCodeBuilder(context, DefaultModel, ActivateAttribute);
        }

        private void UpdateCodeBuilder(CodeGeneratorContext context)
        {
            var chunkUtility = new ChunkInheritanceUtility(context.CodeTreeBuilder.CodeTree,
                                                           DefaultInheritedChunks,
                                                           DefaultModel);
            var inheritedChunks = chunkUtility.GetInheritedChunks(this, _fileSystem, context.SourceFile);
            chunkUtility.MergeInheritedChunks(inheritedChunks);
        }
    }
}
