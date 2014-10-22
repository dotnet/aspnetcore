// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpPaddingBuilder
    {
        private static readonly char[] _newLineChars = { '\r', '\n' };

        private readonly RazorEngineHost _host;

        public CSharpPaddingBuilder(RazorEngineHost host)
        {
            _host = host;
        }

        // Special case for statement padding to account for brace positioning in the editor.
        public string BuildStatementPadding(Span target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            var padding = CalculatePadding(target, generatedStart: 0);

            // We treat statement padding specially so for brace positioning, so that in the following example:
            //   @if (foo > 0)
            //   {
            //   }
            //
            // the braces shows up under the @ rather than under the if.
            if (_host.DesignTimeMode &&
                padding > 0 &&
                target.Previous.Kind == SpanKind.Transition && // target.Previous is guaranteed to not be null if you have padding.
                String.Equals(target.Previous.Content, SyntaxConstants.TransitionString, StringComparison.Ordinal))
            {
                padding--;
            }

            var generatedCode = BuildPaddingInternal(padding);

            return generatedCode;
        }

        public string BuildExpressionPadding(Span target)
        {
            return BuildExpressionPadding(target, generatedStart: 0);
        }

        public string BuildExpressionPadding(Span target, int generatedStart)
        {
            var padding = CalculatePadding(target, generatedStart);

            return BuildPaddingInternal(padding);
        }

        internal int CalculatePadding(Span target, int generatedStart)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            int padding;

            padding = CollectSpacesAndTabs(target, _host.TabSize) - generatedStart;

            // if we add generated text that is longer than the padding we wanted to insert we have no recourse and we have to skip padding
            // example:
            // Razor code at column zero: @somecode()
            // Generated code will be:
            // In design time: __o = somecode();
            // In Run time: Write(somecode());
            //
            // In both cases the padding would have been 1 space to remote the space the @ symbol takes, which will be smaller than the 6 
            // chars the hidden generated code takes.
            if (padding < 0)
            {
                padding = 0;
            }

            return padding;
        }

        private string BuildPaddingInternal(int padding)
        {
            if (_host.DesignTimeMode && _host.IsIndentingWithTabs)
            {
                var spaces = padding % _host.TabSize;
                var tabs = padding / _host.TabSize;

                return new string('\t', tabs) + new string(' ', spaces);
            }
            else
            {
                return new string(' ', padding);
            }
        }

        private static int CollectSpacesAndTabs(Span target, int tabSize)
        {
            var firstSpanInLine = target;

            string currentContent = null;

            while (firstSpanInLine.Previous != null)
            {
                // When scanning previous spans we need to be break down the spans with spaces. The parser combines 
                // whitespace into existing spans so you'll see tabs, newlines etc. within spans.  We only care about
                // the \t in existing spans.
                var previousContent = firstSpanInLine.Previous.Content ?? String.Empty;

                var lastNewLineIndex = previousContent.LastIndexOfAny(_newLineChars);

                if (lastNewLineIndex < 0)
                {
                    firstSpanInLine = firstSpanInLine.Previous;
                }
                else
                {
                    if (lastNewLineIndex != previousContent.Length - 1)
                    {
                        firstSpanInLine = firstSpanInLine.Previous;
                        currentContent = previousContent.Substring(lastNewLineIndex + 1);
                    }

                    break;
                }
            }

            // We need to walk from the beginning of the line, because space + tab(tabSize) = tabSize columns, but tab(tabSize) + space = tabSize+1 columns.
            var currentSpanInLine = firstSpanInLine;

            if (currentContent == null)
            {
                currentContent = currentSpanInLine.Content;
            }

            var padding = 0;
            while (currentSpanInLine != target)
            {
                if (currentContent != null)
                {
                    for (int i = 0; i < currentContent.Length; i++)
                    {
                        if (currentContent[i] == '\t')
                        {
                            // Example:
                            // <space><space><tab><tab>:
                            // iter 1) 1
                            // iter 2) 2
                            // iter 3) 4 = 2 + (4 - 2)
                            // iter 4) 8 = 4 + (4 - 0)
                            padding = padding + (tabSize - (padding % tabSize));
                        }
                        else
                        {
                            padding++;
                        }
                    }
                }

                currentSpanInLine = currentSpanInLine.Next;
                currentContent = currentSpanInLine.Content;
            }

            return padding;
        }
    }
}
