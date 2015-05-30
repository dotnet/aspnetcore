// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
#if NET45
using Microsoft.AspNet.FileProviders;
#endif
using Microsoft.AspNet.Mvc.Razor.Directives;
using Microsoft.AspNet.Mvc.Razor.Internal;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Chunks;
using Microsoft.AspNet.Razor.CodeGenerators;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MvcRazorHost : RazorEngineHost, IMvcRazorHost
    {
        private const string BaseType = "Microsoft.AspNet.Mvc.Razor.RazorPage";
        private const string HtmlHelperPropertyName = "Html";

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
            new InjectChunk("Microsoft.AspNet.Mvc.Rendering.IHtmlHelper<TModel>", HtmlHelperPropertyName),
            new InjectChunk("Microsoft.AspNet.Mvc.Rendering.IJsonHelper", "Json"),
            new InjectChunk("Microsoft.AspNet.Mvc.IViewComponentHelper", "Component"),
            new InjectChunk("Microsoft.AspNet.Mvc.IUrlHelper", "Url"),
        };

        // CodeGenerationContext.DefaultBaseClass is set to MyBaseType<dynamic>.
        // This field holds the type name without the generic decoration (MyBaseType)
        private readonly string _baseType;
        private readonly IChunkTreeCache _chunkTreeCache;
        private readonly RazorPathNormalizer _pathNormalizer;
        private ChunkInheritanceUtility _chunkInheritanceUtility;

        internal MvcRazorHost(IChunkTreeCache chunkTreeCache, RazorPathNormalizer pathNormalizer)
            : base(new CSharpRazorCodeLanguage())
        {
            _pathNormalizer = pathNormalizer;
            _baseType = BaseType;
            _chunkTreeCache = chunkTreeCache;

            TagHelperDescriptorResolver = new TagHelperDescriptorResolver();
            DefaultBaseClass = BaseType + "<" + DefaultModel + ">";
            DefaultNamespace = "Asp";
            // Enable instrumentation by default to allow precompiled views to work with BrowserLink.
            EnableInstrumentation = true;
            GeneratedClassContext = new GeneratedClassContext(
                executeMethodName: "ExecuteAsync",
                writeMethodName: "Write",
                writeLiteralMethodName: "WriteLiteral",
                writeToMethodName: "WriteTo",
                writeLiteralToMethodName: "WriteLiteralTo",
                templateTypeName: "Microsoft.AspNet.Mvc.Razor.HelperResult",
                defineSectionMethodName: "DefineSection",
                generatedTagHelperContext: new GeneratedTagHelperContext
                {
                    ExecutionContextTypeName = typeof(TagHelperExecutionContext).FullName,
                    ExecutionContextAddMethodName = nameof(TagHelperExecutionContext.Add),
                    ExecutionContextAddTagHelperAttributeMethodName =
                        nameof(TagHelperExecutionContext.AddTagHelperAttribute),
                    ExecutionContextAddHtmlAttributeMethodName = nameof(TagHelperExecutionContext.AddHtmlAttribute),
                    ExecutionContextAddMinimizedHtmlAttributeMethodName =
                        nameof(TagHelperExecutionContext.AddMinimizedHtmlAttribute),
                    ExecutionContextOutputPropertyName = nameof(TagHelperExecutionContext.Output),

                    RunnerTypeName = typeof(TagHelperRunner).FullName,
                    RunnerRunAsyncMethodName = nameof(TagHelperRunner.RunAsync),

                    ScopeManagerTypeName = typeof(TagHelperScopeManager).FullName,
                    ScopeManagerBeginMethodName = nameof(TagHelperScopeManager.Begin),
                    ScopeManagerEndMethodName = nameof(TagHelperScopeManager.End),

                    TagHelperContentTypeName = nameof(TagHelperContent),

                    // Can't use nameof because RazorPage is not accessible here.
                    CreateTagHelperMethodName = "CreateTagHelper",
                    FormatInvalidIndexerAssignmentMethodName = "InvalidTagHelperIndexerAssignment",
                    StartTagHelperWritingScopeMethodName = "StartTagHelperWritingScope",
                    EndTagHelperWritingScopeMethodName = "EndTagHelperWritingScope",

                    WriteTagHelperAsyncMethodName = "WriteTagHelperAsync",
                    WriteTagHelperToAsyncMethodName = "WriteTagHelperToAsync",

                    // Can't use nameof because IHtmlHelper is (also) not accessible here.
                    MarkAsHtmlEncodedMethodName = HtmlHelperPropertyName + ".Raw",
                })
            {
                ResolveUrlMethodName = "Href",
                BeginContextMethodName = "BeginContext",
                EndContextMethodName = "EndContext"
            };

            foreach (var ns in _defaultNamespaces)
            {
                NamespaceImports.Add(ns);
            }
        }

#if NET45
        /// <summary>
        /// Initializes a new instance of <see cref="MvcRazorHost"/> with the specified  <paramref name="root"/>.
        /// </summary>
        /// <param name="root">The path to the application base.</param>
        // Note: This constructor is used by tooling and is created once for each
        // Razor page that is loaded. Consequently, each loaded page has its own copy of
        // the ChunkTreeCache, but this ok - having a shared ChunkTreeCache per application in tooling
        // is problematic to manage.
        public MvcRazorHost(string root)
            : this(new DefaultChunkTreeCache(new PhysicalFileProvider(root)), new DesignTimeRazorPathNormalizer(root))
        {
        }
#endif
        /// <summary>
        /// Initializes a new instance of <see cref="MvcRazorHost"/> using the specified <paramref name="chunkTreeCache"/>.
        /// </summary>
        /// <param name="chunkTreeCache">An <see cref="IChunkTreeCache"/> rooted at the application base path.</param>
        public MvcRazorHost(IChunkTreeCache chunkTreeCache)
            : this(chunkTreeCache, new RazorPathNormalizer())
        {
        }

        /// <summary>
        /// Gets the model type used by default when no model is specified.
        /// </summary>
        /// <remarks>This value is used as the generic type argument for the base type </remarks>
        public virtual string DefaultModel
        {
            get { return "dynamic"; }
        }

        /// <inheritdoc />
        public string MainClassNamePrefix
        {
            get { return "ASPV_"; }
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
        public virtual string InjectAttribute
        {
            get { return "Microsoft.AspNet.Mvc.Razor.Internal.RazorInjectAttribute"; }
        }

        /// <summary>
        /// Gets the type name used to represent <see cref="ITagHelper"/> model expression properties.
        /// </summary>
        public virtual string ModelExpressionType
        {
            get { return "Microsoft.AspNet.Mvc.Rendering.ModelExpression"; }
        }

        /// <summary>
        /// Gets the method name used to create model expressions.
        /// </summary>
        public virtual string CreateModelExpressionMethod
        {
            get { return "CreateModelExpression"; }
        }

        // Internal for testing
        internal ChunkInheritanceUtility ChunkInheritanceUtility
        {
            get
            {
                if (_chunkInheritanceUtility == null)
                {
                    // This needs to be lazily evaluated to support DefaultInheritedChunks being virtual.
                    _chunkInheritanceUtility = new ChunkInheritanceUtility(this, _chunkTreeCache, DefaultInheritedChunks);
                }

                return _chunkInheritanceUtility;
            }
            set
            {
                _chunkInheritanceUtility = value;
            }
        }

        /// <inheritdoc />
        public GeneratorResults GenerateCode(string rootRelativePath, Stream inputStream)
        {
            // Adding a prefix so that the main view class can be easily identified.
            var className = MainClassNamePrefix + ParserHelpers.SanitizeClassName(rootRelativePath);
            var engine = new RazorTemplateEngine(this);
            return engine.GenerateCode(inputStream, className, DefaultNamespace, rootRelativePath);
        }

        /// <inheritdoc />
        public override RazorParser DecorateRazorParser([NotNull] RazorParser razorParser, string sourceFileName)
        {
            sourceFileName = _pathNormalizer.NormalizePath(sourceFileName);

            var inheritedChunkTrees = ChunkInheritanceUtility.GetInheritedChunkTrees(sourceFileName);
            return new MvcRazorParser(razorParser, inheritedChunkTrees, DefaultInheritedChunks, ModelExpressionType);
        }

        /// <inheritdoc />
        public override ParserBase DecorateCodeParser([NotNull] ParserBase incomingCodeParser)
        {
            return new MvcRazorCodeParser(_baseType);
        }

        /// <inheritdoc />
        public override CodeGenerator DecorateCodeGenerator(
            [NotNull] CodeGenerator incomingGenerator,
            [NotNull] CodeGeneratorContext context)
        {
            // Need the normalized path to resolve inherited chunks only. Full paths are needed for generated Razor
            // files checksum and line pragmas to enable DesignTime debugging.
            var normalizedPath = _pathNormalizer.NormalizePath(context.SourceFile);
            var inheritedChunks = ChunkInheritanceUtility.GetInheritedChunkTrees(normalizedPath);

            ChunkInheritanceUtility.MergeInheritedChunkTrees(
                context.ChunkTreeBuilder.ChunkTree,
                inheritedChunks,
                DefaultModel);

            return new MvcCSharpCodeGenerator(
                context,
                DefaultModel,
                InjectAttribute,
                new GeneratedTagHelperAttributeContext
                {
                    ModelExpressionTypeName = ModelExpressionType,
                    CreateModelExpressionMethodName = CreateModelExpressionMethod
                });
        }
    }
}
