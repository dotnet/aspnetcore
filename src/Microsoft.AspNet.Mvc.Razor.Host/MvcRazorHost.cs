// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if NET451
using Microsoft.AspNet.FileProviders;
#endif
using Microsoft.AspNet.Mvc.Razor.Directives;
using Microsoft.AspNet.Mvc.Razor.Internal;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Chunks;
using Microsoft.AspNet.Razor.CodeGenerators;
using Microsoft.AspNet.Razor.Compilation.TagHelpers;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;

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
        private static readonly Chunk[] _defaultInheritedChunks = new Chunk[]
        {
            new InjectChunk("Microsoft.AspNet.Mvc.Rendering.IHtmlHelper<TModel>", HtmlHelperPropertyName),
            new InjectChunk("Microsoft.AspNet.Mvc.Rendering.IJsonHelper", "Json"),
            new InjectChunk("Microsoft.AspNet.Mvc.IViewComponentHelper", "Component"),
            new InjectChunk("Microsoft.AspNet.Mvc.IUrlHelper", "Url"),
            new AddTagHelperChunk
            {
                LookupText = "Microsoft.AspNet.Mvc.Razor.TagHelpers.UrlResolutionTagHelper, Microsoft.AspNet.Mvc.Razor"
            },
            new SetBaseTypeChunk
            {
                // Microsoft.Aspnet.Mvc.Razor.RazorPage<TModel>
                TypeName = $"{BaseType}<{ChunkHelper.TModelToken}>",
                // Set the Start to Undefined to prevent Razor design time code generation from rendering a line mapping
                // for this chunk.
                Start = SourceLocation.Undefined
            }
        };

        // CodeGenerationContext.DefaultBaseClass is set to MyBaseType<dynamic>.
        private readonly IChunkTreeCache _chunkTreeCache;
        private readonly RazorPathNormalizer _pathNormalizer;
        private ChunkInheritanceUtility _chunkInheritanceUtility;
        private ITagHelperDescriptorResolver _tagHelperDescriptorResolver;

        internal MvcRazorHost(IChunkTreeCache chunkTreeCache, RazorPathNormalizer pathNormalizer)
            : base(new CSharpRazorCodeLanguage())
        {
            _pathNormalizer = pathNormalizer;
            _chunkTreeCache = chunkTreeCache;

            DefaultBaseClass = $"{BaseType}<{ChunkHelper.TModelToken}>";
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

                    TagHelperContentTypeName = typeof(TagHelperContent).FullName,

                    // Can't use nameof because RazorPage is not accessible here.
                    CreateTagHelperMethodName = "CreateTagHelper",
                    FormatInvalidIndexerAssignmentMethodName = "InvalidTagHelperIndexerAssignment",
                    StartTagHelperWritingScopeMethodName = "StartTagHelperWritingScope",
                    EndTagHelperWritingScopeMethodName = "EndTagHelperWritingScope",

                    // Can't use nameof because IHtmlHelper is (also) not accessible here.
                    MarkAsHtmlEncodedMethodName = HtmlHelperPropertyName + ".Raw",
                    BeginAddHtmlAttributeValuesMethodName = "BeginAddHtmlAttributeValues",
                    EndAddHtmlAttributeValuesMethodName = "EndAddHtmlAttributeValues",
                    AddHtmlAttributeValueMethodName = "AddHtmlAttributeValue",
                    HtmlEncoderPropertyName = "HtmlEncoder",
                    TagHelperContentGetContentMethodName = nameof(TagHelperContent.GetContent),
                    TagHelperOutputIsContentModifiedPropertyName = nameof(TagHelperOutput.IsContentModified),
                    TagHelperOutputContentPropertyName = nameof(TagHelperOutput.Content),
                    TagHelperOutputGetChildContentAsyncMethodName = nameof(TagHelperExecutionContext.GetChildContentAsync)
                })
            {
                BeginContextMethodName = "BeginContext",
                EndContextMethodName = "EndContext"
            };

            foreach (var ns in _defaultNamespaces)
            {
                NamespaceImports.Add(ns);
            }
        }

#if NET451
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

        /// <inheritdoc />
        public override ITagHelperDescriptorResolver TagHelperDescriptorResolver
        {
            get
            {
                // The initialization of the _tagHelperDescriptorResolver needs to be lazy to allow for the setting
                // of DesignTimeMode.
                if (_tagHelperDescriptorResolver == null)
                {
                    _tagHelperDescriptorResolver = new TagHelperDescriptorResolver(DesignTimeMode);
                }

                return _tagHelperDescriptorResolver;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _tagHelperDescriptorResolver = value;
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

        /// <summary>
        /// Locates and parses _ViewImports.cshtml files applying to the given <paramref name="sourceFileName"/> to
        /// create <see cref="ChunkTreeResult"/>s.
        /// </summary>
        /// <param name="sourceFileName">The path to a Razor file to locate _ViewImports.cshtml for.</param>
        /// <returns>Inherited <see cref="ChunkTreeResult"/>s.</returns>
        public IReadOnlyList<ChunkTreeResult> GetInheritedChunkTreeResults(string sourceFileName)
        {
            if (sourceFileName == null)
            {
                throw new ArgumentNullException(nameof(sourceFileName));
            }

            // Need the normalized path to resolve inherited chunks only. Full paths are needed for generated Razor
            // files checksum and line pragmas to enable DesignTime debugging.
            var normalizedPath = _pathNormalizer.NormalizePath(sourceFileName);

            return ChunkInheritanceUtility.GetInheritedChunkTreeResults(normalizedPath);
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
        public override RazorParser DecorateRazorParser(RazorParser razorParser, string sourceFileName)
        {
            if (razorParser == null)
            {
                throw new ArgumentNullException(nameof(razorParser));
            }

            var inheritedChunkTrees = GetInheritedChunkTrees(sourceFileName);

            return new MvcRazorParser(razorParser, inheritedChunkTrees, DefaultInheritedChunks, ModelExpressionType);
        }

        /// <inheritdoc />
        public override ParserBase DecorateCodeParser(ParserBase incomingCodeParser)
        {
            if (incomingCodeParser == null)
            {
                throw new ArgumentNullException(nameof(incomingCodeParser));
            }

            return new MvcRazorCodeParser();
        }

        /// <inheritdoc />
        public override CodeGenerator DecorateCodeGenerator(
            CodeGenerator incomingGenerator,
            CodeGeneratorContext context)
        {
            if (incomingGenerator == null)
            {
                throw new ArgumentNullException(nameof(incomingGenerator));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var inheritedChunkTrees = GetInheritedChunkTrees(context.SourceFile);

            ChunkInheritanceUtility.MergeInheritedChunkTrees(
                context.ChunkTreeBuilder.Root,
                inheritedChunkTrees,
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

        private IReadOnlyList<ChunkTree> GetInheritedChunkTrees(string sourceFileName)
        {
            var inheritedChunkTrees = GetInheritedChunkTreeResults(sourceFileName)
                .Select(result => result.ChunkTree)
                .ToList();

            return inheritedChunkTrees;
        }
    }
}
