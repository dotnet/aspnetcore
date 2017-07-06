// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal partial class ParserContext
    {
        public ParserContext(RazorSourceDocument source, RazorParserOptions options)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            SourceDocument = source;
            var chars = new char[source.Length];
            source.CopyTo(0, chars, 0, source.Length);

            Source = new SeekableTextReader(chars, source.FilePath);
            DesignTimeMode = options.DesignTime;
            ParseLeadingDirectives = options.ParseLeadingDirectives;
            Builder = new SyntaxTreeBuilder();
            ErrorSink = new ErrorSink();
            SeenDirectives = new HashSet<string>(StringComparer.Ordinal);
        }

        public SyntaxTreeBuilder Builder { get; }

        public ErrorSink ErrorSink { get; set; }

        public HashSet<string> SeenDirectives { get; }

        public ITextDocument Source { get; }

        public RazorSourceDocument SourceDocument { get; }

        public bool DesignTimeMode { get; }

        public bool ParseLeadingDirectives { get; }

        public bool WhiteSpaceIsSignificantToAncestorBlock { get; set; }

        public bool NullGenerateWhitespaceAndNewLine { get; set; }

        public bool EndOfFile
        {
            get { return Source.Peek() == -1; }
        }
    }

    // Debug Helpers

#if DEBUG
    [DebuggerDisplay("{Unparsed}")]
    internal partial class ParserContext
    {
        private const int InfiniteLoopCountThreshold = 1000;
        private int _infiniteLoopGuardCount = 0;
        private SourceLocation? _infiniteLoopGuardLocation = null;

        internal string Unparsed
        {
            get
            {
                var remaining = ((TextReader)Source).ReadToEnd();
                Source.Position -= remaining.Length;
                return remaining;
            }
        }

        private bool CheckInfiniteLoop()
        {
            // Infinite loop guard
            //  Basically, if this property is accessed 1000 times in a row without having advanced the source reader to the next position, we
            //  cause a parser error
            if (_infiniteLoopGuardLocation != null)
            {
                if (Source.Location.Equals(_infiniteLoopGuardLocation.Value))
                {
                    _infiniteLoopGuardCount++;
                    if (_infiniteLoopGuardCount > InfiniteLoopCountThreshold)
                    {
                        Debug.Fail("An internal parser error is causing an infinite loop at this location.");

                        return true;
                    }
                }
                else
                {
                    _infiniteLoopGuardCount = 0;
                }
            }
            _infiniteLoopGuardLocation = Source.Location;
            return false;
        }
    }
#endif
}
