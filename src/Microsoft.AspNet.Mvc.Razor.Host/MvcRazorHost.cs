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
using Microsoft.AspNet.Razor.Runtime.TagHelpers;

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
        private ChunkInheritanceUtility _chunkInheritanceUtility;

#if NET45
        /// <summary>
        /// Initializes a new instance of <see cref="MvcRazorHost"/> with the specified
        /// <param name="root"/>.
        /// </summary>
        /// <param name="root">The path to the application base.</param>
        public MvcRazorHost(string root) :
            this(new PhysicalFileSystem(root))
        {
        }
#endif
        /// <summary>
        /// Initializes a new instance of <see cref="MvcRazorHost"/> using the specified <paramref name="fileSystem"/>.
        /// </summary>
        /// <param name="fileSystem">A <see cref="IFileSystem"/> rooted at the application base path.</param>
        public MvcRazorHost(IFileSystem fileSystem)
            : base(new CSharpRazorCodeLanguage())
        {
            _fileSystem = fileSystem;
            _baseType = BaseType;

            TagHelperDescriptorResolver = new TagHelperDescriptorResolver();
            DefaultBaseClass = BaseType + '<' + DefaultModel + '>';
            DefaultNamespace = "Asp";
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
                    ExecutionContextOutputPropertyName = nameof(TagHelperExecutionContext.Output),

                    RunnerTypeName = typeof(TagHelperRunner).FullName,
                    RunnerRunAsyncMethodName = nameof(TagHelperRunner.RunAsync),

                    ScopeManagerTypeName = typeof(TagHelperScopeManager).FullName,
                    ScopeManagerBeginMethodName = nameof(TagHelperScopeManager.Begin),
                    ScopeManagerEndMethodName = nameof(TagHelperScopeManager.End),

                    OutputGenerateStartTagMethodName = nameof(TagHelperOutput.GenerateStartTag),
                    OutputGenerateContentMethodName = nameof(TagHelperOutput.GenerateContent),
                    OutputGenerateEndTagMethodName = nameof(TagHelperOutput.GenerateEndTag),

                    // Can't use nameof because RazorPage is not accessible here.
                    CreateTagHelperMethodName = "CreateTagHelper",
                    StartWritingScopeMethodName = "StartWritingScope",
                    EndWritingScopeMethodName = "EndWritingScope",
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
        public virtual string ActivateAttribute
        {
            get { return "Microsoft.AspNet.Mvc.ActivateAttribute"; }
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

        private ChunkInheritanceUtility ChunkInheritanceUtility
        {
            get
            {
                if (_chunkInheritanceUtility == null)
                {
                    // This needs to be lazily evaluated to support DefaultInheritedChunks being virtual.
                    _chunkInheritanceUtility = new ChunkInheritanceUtility(this, _fileSystem, DefaultInheritedChunks);
                }

                return _chunkInheritanceUtility;
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
            var inheritedChunks = ChunkInheritanceUtility.GetInheritedChunks(sourceFileName);
            return new MvcRazorParser(razorParser, inheritedChunks);
        }

        /// <inheritdoc />
        public override ParserBase DecorateCodeParser([NotNull] ParserBase incomingCodeParser)
        {
            return new MvcRazorCodeParser(_baseType);
        }

        /// <inheritdoc />
        public override CodeBuilder DecorateCodeBuilder([NotNull] CodeBuilder incomingBuilder,
                                                        [NotNull] CodeBuilderContext context)
        {
            var inheritedChunks = ChunkInheritanceUtility.GetInheritedChunks(context.SourceFile);

            ChunkInheritanceUtility.MergeInheritedChunks(context.CodeTreeBuilder.CodeTree,
                                                         inheritedChunks,
                                                         DefaultModel);

            return new MvcCSharpCodeBuilder(context,
                                            DefaultModel,
                                            ActivateAttribute,
                                            new GeneratedTagHelperAttributeContext
                                            {
                                                ModelExpressionTypeName = ModelExpressionType,
                                                CreateModelExpressionMethodName = CreateModelExpressionMethod
                                            });
        }
    }
}
