// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal partial class ParserContext
    {
        public ParserContext(ITextDocument source, bool designTime)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Source = source;
            DesignTimeMode = designTime;
            Builder = new SyntaxTreeBuilder();
            ErrorSink = new ErrorSink();
        }

        public SyntaxTreeBuilder Builder { get; }

        public ErrorSink ErrorSink { get; }

        public ITextDocument Source { get; }

        public bool DesignTimeMode { get; }

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
