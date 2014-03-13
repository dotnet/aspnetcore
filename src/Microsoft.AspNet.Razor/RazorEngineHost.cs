// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Generator.Compiler.CSharp;
using Microsoft.AspNet.Razor.Parser;

namespace Microsoft.AspNet.Razor
{
    /// <summary>
    /// Defines the environment in which a Razor template will live
    /// </summary>
    /// <remarks>
    /// The host defines the following things:
    /// * What method names will be used for rendering markup, expressions etc.  For example "Write", "WriteLiteral"
    /// * The namespace imports to be added to every page generated via this host
    /// * The default Base Class to inherit the generated class from
    /// * The default Class Name and Namespace for the generated class (can be overridden by parameters in RazorTemplateEngine.GeneratedCode)
    /// * The language of the code in a Razor page
    /// * The markup, code parsers and code generators to use (the system will select defaults, but a Host gets a change to augment them)
    ///     ** See DecorateNNN methods
    /// * Additional code to add to the generated code (see PostProcessGeneratedCode)
    /// </remarks>
    public class RazorEngineHost
    {
        internal const string InternalDefaultClassName = "__CompiledTemplate";
        internal const string InternalDefaultNamespace = "Razor";

        private bool _instrumentationActive = false;
        private Func<ParserBase> _markupParserFactory;

        private int _tabSize = 4;

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "The code path is safe, it is a property setter and not dependent on other state")]
        protected RazorEngineHost()
        {
            GeneratedClassContext = GeneratedClassContext.Default;
            NamespaceImports = new HashSet<string>();
            DesignTimeMode = false;
            DefaultNamespace = InternalDefaultNamespace;
            DefaultClassName = InternalDefaultClassName;
            EnableInstrumentation = false;
        }

        /// <summary>
        /// Creates a host which uses the specified code language and the HTML markup language
        /// </summary>
        /// <param name="codeLanguage">The code language to use</param>
        public RazorEngineHost(RazorCodeLanguage codeLanguage)
            : this(codeLanguage, () => new HtmlMarkupParser())
        {
        }

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "The code path is safe, it is a property setter and not dependent on other state")]
        public RazorEngineHost(RazorCodeLanguage codeLanguage, Func<ParserBase> markupParserFactory)
            : this()
        {
            if (codeLanguage == null)
            {
                throw new ArgumentNullException("codeLanguage");
            }
            if (markupParserFactory == null)
            {
                throw new ArgumentNullException("markupParserFactory");
            }

            CodeLanguage = codeLanguage;
            _markupParserFactory = markupParserFactory;
        }

        /// <summary>
        /// Details about the methods and types that should be used to generate code for Razor constructs
        /// </summary>
        public virtual GeneratedClassContext GeneratedClassContext { get; set; }

        /// <summary>
        /// A list of namespaces to import in the generated file
        /// </summary>
        public virtual ISet<string> NamespaceImports { get; private set; }

        /// <summary>
        /// The base-class of the generated class
        /// </summary>
        public virtual string DefaultBaseClass { get; set; }

        /// <summary>
        /// Indiciates if the parser and code generator should run in design-time mode
        /// </summary>
        public virtual bool DesignTimeMode { get; set; }

        /// <summary>
        /// The name of the generated class
        /// </summary>
        public virtual string DefaultClassName { get; set; }

        /// <summary>
        /// The namespace which will contain the generated class
        /// </summary>
        public virtual string DefaultNamespace { get; set; }

        /// <summary>
        /// Boolean indicating if helper methods should be instance methods or static methods
        /// </summary>
        public virtual bool StaticHelpers { get; set; }

        /// <summary>
        /// The language of the code within the Razor template.
        /// </summary>
        public virtual RazorCodeLanguage CodeLanguage { get; protected set; }

        /// <summary>
        /// Boolean indicating if instrumentation code should be injected into the output page
        /// </summary>
        public virtual bool EnableInstrumentation
        {
            // Always disable instrumentation in DesignTimeMode.
            get { return !DesignTimeMode && _instrumentationActive; }
            set { _instrumentationActive = value; }
        }

        /// <summary>
        /// Gets or sets whether the design time editor is using tabs or spaces for indentation.
        /// </summary>
        public virtual bool IsIndentingWithTabs { get; set; }

        /// <summary>
        /// Tab size used by the hosting editor, when indenting with tabs.
        /// </summary>
        public virtual int TabSize
        {
            get
            {
                return _tabSize;
            }

            set
            {
                _tabSize = Math.Max(value, 1);
            }
        }

        /// <summary>
        /// Gets or sets the path to use for this document when generating Instrumentation calls
        /// </summary>
        public virtual string InstrumentedSourceFilePath { get; set; }

        /// <summary>
        /// Constructs the markup parser.  Must return a new instance on EVERY call to ensure thread-safety
        /// </summary>
        public virtual ParserBase CreateMarkupParser()
        {
            if (_markupParserFactory != null)
            {
                return _markupParserFactory();
            }
            return null;
        }

        /// <summary>
        /// Gets an instance of the code parser and is provided an opportunity to decorate or replace it
        /// </summary>
        /// <param name="incomingCodeParser">The code parser</param>
        /// <returns>Either the same code parser, after modifications, or a different code parser</returns>
        public virtual ParserBase DecorateCodeParser(ParserBase incomingCodeParser)
        {
            if (incomingCodeParser == null)
            {
                throw new ArgumentNullException("incomingCodeParser");
            }
            return incomingCodeParser;
        }

        /// <summary>
        /// Gets an instance of the markup parser and is provided an opportunity to decorate or replace it
        /// </summary>
        /// <param name="incomingMarkupParser">The markup parser</param>
        /// <returns>Either the same markup parser, after modifications, or a different markup parser</returns>
        public virtual ParserBase DecorateMarkupParser(ParserBase incomingMarkupParser)
        {
            if (incomingMarkupParser == null)
            {
                throw new ArgumentNullException("incomingMarkupParser");
            }
            return incomingMarkupParser;
        }

        /// <summary>
        /// Gets an instance of the code generator and is provided an opportunity to decorate or replace it
        /// </summary>
        /// <param name="incomingCodeGenerator">The code generator</param>
        /// <returns>Either the same code generator, after modifications, or a different code generator</returns>
        public virtual RazorCodeGenerator DecorateCodeGenerator(RazorCodeGenerator incomingCodeGenerator)
        {
            if (incomingCodeGenerator == null)
            {
                throw new ArgumentNullException("incomingCodeGenerator");
            }
            return incomingCodeGenerator;
        }
    }
}
