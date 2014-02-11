using System;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpPaddingBuilder
    {
        private static readonly char[] _newLineChars = { '\r', '\n' };

        private RazorEngineHost _host;

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

            int padding = CalculatePadding(target);

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

            string generatedCode = BuildPaddingInternal(padding);

            return generatedCode;
        }

        public string BuildExpressionPadding(Span target)
        {
            int padding = CalculatePadding(target);

            return BuildPaddingInternal(padding);
        }

        internal int CalculatePadding(Span target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            return CollectSpacesAndTabs(target, _host.TabSize);
        }

        private string BuildPaddingInternal(int padding)
        {
            if (_host.DesignTimeMode && _host.IsIndentingWithTabs)
            {
                int spaces = padding % _host.TabSize;
                int tabs = padding / _host.TabSize;

                return new string('\t', tabs) + new string(' ', spaces);
            }
            else
            {
                return new string(' ', padding);
            }
        }

        private static int CollectSpacesAndTabs(Span target, int tabSize)
        {
            Span firstSpanInLine = target;

            string currentContent = null;

            while (firstSpanInLine.Previous != null)
            {
                // When scanning previous spans we need to be break down the spans with spaces. The parser combines 
                // whitespace into existing spans so you'll see tabs, newlines etc. within spans.  We only care about
                // the \t in existing spans.
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
}
