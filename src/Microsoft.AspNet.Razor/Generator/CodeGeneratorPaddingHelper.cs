// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    #if NET45
    internal static class CodeGeneratorPaddingHelper
    {
        private static readonly char[] _newLineChars = { '\r', '\n' };

        // there is some duplicity of code here, but its very simple and since this is a host path, I'd rather not create another class to encapsulate the data.
        public static int PaddingCharCount(RazorEngineHost host, Span target, int generatedStart)
        {
            int padding = CalculatePadding(host, target, generatedStart);

            if (host.DesignTimeMode && host.IsIndentingWithTabs)
            {
                int spaces;
                int tabs = Math.DivRem(padding, host.TabSize, out spaces);

                return tabs + spaces;
            }
            else
            {
                return padding;
            }
        }

        // Special case for statement padding to account for brace positioning in the editor.
        public static string PadStatement(RazorEngineHost host, string code, Span target, ref int startGeneratedCode, out int paddingCharCount)
        {
            if (host == null)
            {
                throw new ArgumentNullException("host");
            }

            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            // We are passing 0 rather than startgeneratedcode intentionally (keeping v2 behavior).
            int padding = CalculatePadding(host, target, 0);

            // We treat statement padding specially so for brace positioning, so that in the following example:
            //   @if (foo > 0)
            //   {
            //   }
            //
            // the braces shows up under the @ rather than under the if.
            if (host.DesignTimeMode &&
                padding > 0 &&
                target.Previous.Kind == SpanKind.Transition && // target.Previous is guaranteed to be none null if you got any padding.
                String.Equals(target.Previous.Content, SyntaxConstants.TransitionString))
            {
                padding--;
                startGeneratedCode--;
            }

            string generatedCode = PadInternal(host, code, padding, out paddingCharCount);

            return generatedCode;
        }

        public static string Pad(RazorEngineHost host, string code, Span target, out int paddingCharCount)
        {
            int padding = CalculatePadding(host, target, 0);

            return PadInternal(host, code, padding, out paddingCharCount);
        }

        public static string Pad(RazorEngineHost host, string code, Span target, int generatedStart, out int paddingCharCount)
        {
            int padding = CalculatePadding(host, target, generatedStart);

            return PadInternal(host, code, padding, out paddingCharCount);
        }

        // internal for unit testing only, not intended to be used directly in code
        internal static int CalculatePadding(RazorEngineHost host, Span target, int generatedStart)
        {
            if (host == null)
            {
                throw new ArgumentNullException("host");
            }

            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            int padding;

            padding = CollectSpacesAndTabs(target, host.TabSize) - generatedStart;

            // if we add generated text that is longer than the padding we wanted to insert we have no recourse and we have to skip padding
            // example:
            // Razor code at column zero: @somecode()
            // Generated code will be:
            // In design time: __o = somecode();
            // In Run time: Write(somecode());
            //
            // In both cases the padding would have been 1 space to remote the space the @ symbol takes, which will be smaller than the 6 chars the hidden generated code takes.
            if (padding < 0)
            {
                padding = 0;
            }

            return padding;
        }

        private static string PadInternal(RazorEngineHost host, string code, int padding, out int paddingCharCount)
        {
            if (host.DesignTimeMode && host.IsIndentingWithTabs)
            {
                int spaces;
                int tabs = Math.DivRem(padding, host.TabSize, out spaces);

                paddingCharCount = tabs + spaces;

                return new string('\t', tabs) + new string(' ', spaces) + code;
            }
            else
            {
                paddingCharCount = padding;
                return code.PadLeft(padding + code.Length, ' ');
            }
        }

        private static int CollectSpacesAndTabs(Span target, int tabSize)
        {
            Span firstSpanInLine = target;

            string currentContent = null;

            while (firstSpanInLine.Previous != null)
            {
                // When scanning previous spans we need to be break down the spans with spaces.
                // Because the parser doesn't so for example a span looking like \n\n\t needs to be broken down, and we should just grab the \t.
                String previousContent = firstSpanInLine.Previous.Content ?? String.Empty;

                int lastNewLineIndex = previousContent.LastIndexOfAny(_newLineChars);

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
            Span currentSpanInLine = firstSpanInLine;

            if (currentContent == null)
            {
                currentContent = currentSpanInLine.Content;
            }

            int padding = 0;
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
#endif
}
