// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Parser.TagHelpers.Internal;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Parser
{
    public class RazorParser
    {
        public RazorParser(ParserBase codeParser, ParserBase markupParser)
        {
            if (codeParser == null)
            {
                throw new ArgumentNullException("codeParser");
            }
            if (markupParser == null)
            {
                throw new ArgumentNullException("markupParser");
            }

            MarkupParser = markupParser;
            CodeParser = codeParser;

            // TODO: As part of https://github.com/aspnet/Razor/issues/111 and 
            // https://github.com/aspnet/Razor/issues/112 pull the provider from some sort of tag helper locator 
            // object.
            var provider = new TagHelperDescriptorProvider(Enumerable.Empty<TagHelperDescriptor>());

            Optimizers = new List<ISyntaxTreeRewriter>()
            {
                // TODO: Modify the below WhiteSpaceRewriter & ConditionalAttributeCollapser to handle 
                // TagHelperBlock's: https://github.com/aspnet/Razor/issues/117

                // Move whitespace from start of expression block to markup
                new WhiteSpaceRewriter(MarkupParser.BuildSpan),
                // Collapse conditional attributes where the entire value is literal
                new ConditionalAttributeCollapser(MarkupParser.BuildSpan),
                // Enables tag helpers
                new TagHelperParseTreeRewriter(provider),
            };
        }

        internal ParserBase CodeParser { get; private set; }
        internal ParserBase MarkupParser { get; private set; }
        internal IList<ISyntaxTreeRewriter> Optimizers { get; private set; }

        public bool DesignTimeMode { get; set; }

        public virtual void Parse(TextReader input, ParserVisitor visitor)
        {
            Parse(new SeekableTextReader(input), visitor);
        }

        public virtual ParserResults Parse(TextReader input)
        {
            return ParseCore(new SeekableTextReader(input));
        }

        public virtual ParserResults Parse(ITextDocument input)
        {
            return ParseCore(input);
        }

#pragma warning disable 0618
        [Obsolete("Lookahead-based readers have been deprecated, use overrides which accept a TextReader or ITextDocument instead")]
        public virtual void Parse(LookaheadTextReader input, ParserVisitor visitor)
        {
            ParserResults results = ParseCore(new SeekableTextReader(input));

            // Replay the results on the visitor
            visitor.Visit(results);
        }

        [Obsolete("Lookahead-based readers have been deprecated, use overrides which accept a TextReader or ITextDocument instead")]
        public virtual ParserResults Parse(LookaheadTextReader input)
        {
            return ParseCore(new SeekableTextReader(input));
        }
#pragma warning restore 0618

        public virtual Task CreateParseTask(TextReader input, Action<Span> spanCallback, Action<RazorError> errorCallback)
        {
            return CreateParseTask(input, new CallbackVisitor(spanCallback, errorCallback));
        }

        public virtual Task CreateParseTask(TextReader input, Action<Span> spanCallback, Action<RazorError> errorCallback, SynchronizationContext context)
        {
            return CreateParseTask(input, new CallbackVisitor(spanCallback, errorCallback) { SynchronizationContext = context });
        }

        public virtual Task CreateParseTask(TextReader input, Action<Span> spanCallback, Action<RazorError> errorCallback, CancellationToken cancelToken)
        {
            return CreateParseTask(input, new CallbackVisitor(spanCallback, errorCallback) { CancelToken = cancelToken });
        }

        public virtual Task CreateParseTask(TextReader input, Action<Span> spanCallback, Action<RazorError> errorCallback, SynchronizationContext context, CancellationToken cancelToken)
        {
            return CreateParseTask(input, new CallbackVisitor(spanCallback, errorCallback)
            {
                SynchronizationContext = context,
                CancelToken = cancelToken
            });
        }

        [SuppressMessage("Microsoft.Web.FxCop", "MW1200:DoNotConstructTaskInstances", Justification = "This rule is not applicable to this assembly.")]
        public virtual Task CreateParseTask(TextReader input,
                                            ParserVisitor consumer)
        {
            return new Task(() =>
            {
                try
                {
                    Parse(input, consumer);
                }
                catch (OperationCanceledException)
                {
                    return; // Just return if we're cancelled.
                }
            });
        }

        private ParserResults ParseCore(ITextDocument input)
        {
            // Setup the parser context
            ParserContext context = new ParserContext(input, CodeParser, MarkupParser, MarkupParser)
            {
                DesignTimeMode = DesignTimeMode
            };

            MarkupParser.Context = context;
            CodeParser.Context = context;

            // Execute the parse
            MarkupParser.ParseDocument();

            // Get the result
            ParserResults results = context.CompleteParse();

            // Rewrite whitespace if supported
            Block current = results.Document;
            foreach (ISyntaxTreeRewriter rewriter in Optimizers)
            {
                current = rewriter.Rewrite(current);
            }

            // Link the leaf nodes into a chain
            Span prev = null;
            foreach (Span node in current.Flatten())
            {
                node.Previous = prev;
                if (prev != null)
                {
                    prev.Next = node;
                }
                prev = node;
            }

            // Return the new result
            return new ParserResults(current, results.ParserErrors);
        }
    }
}
