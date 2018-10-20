// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class GeneratedCodeContainer
    {
        public event EventHandler<TextChangeEventArgs> GeneratedCodeChanged;

        private SourceText _source;
        private VersionStamp? _sourceVersion;
        private RazorCSharpDocument _output;
        private DocumentSnapshot _latestDocument;

        private readonly object _setOutputLock = new object();
        private readonly TextContainer _textContainer;

        public GeneratedCodeContainer()
        {
            _textContainer = new TextContainer(_setOutputLock);
            _textContainer.TextChanged += TextContainer_TextChanged;
        }

        public SourceText Source
        {
            get
            {
                lock (_setOutputLock)
                {
                    return _source;
                }
            }
        }

        public VersionStamp SourceVersion
        {
            get
            {
                lock (_setOutputLock)
                {
                    return _sourceVersion.Value;
                }
            }
        }

        public RazorCSharpDocument Output
        {
            get
            {
                lock (_setOutputLock)
                {
                    return _output;
                }
            }
        }

        public DocumentSnapshot LatestDocument
        {
            get
            {
                lock (_setOutputLock)
                {
                    return _latestDocument;
                }
            }
        }

        public SourceTextContainer SourceTextContainer
        {
            get
            {
                lock (_setOutputLock)
                {
                    return _textContainer;
                }
            }
        }

        public void SetOutput(RazorCSharpDocument csharpDocument, DefaultDocumentSnapshot document)
        {
            lock (_setOutputLock)
            {
                if (!document.TryGetTextVersion(out var version))
                {
                    Debug.Fail("The text version should have already been evaluated.");
                    return;
                }

                if (_sourceVersion.HasValue &&
                    _sourceVersion != version &&
                    _sourceVersion == SourceVersion.GetNewerVersion(version))
                {
                    // Latest document is newer than the provided document.
                    return;
                }

                if (!document.TryGetText(out var source))
                {
                    Debug.Fail("The text should have already been evaluated.");
                    return;
                }

                _source = source;
                _sourceVersion = version;
                _output = csharpDocument;
                _latestDocument = document;
                _textContainer.SetText(SourceText.From(Output.GeneratedCode));
            }
        }

        private void TextContainer_TextChanged(object sender, TextChangeEventArgs args)
        {
            GeneratedCodeChanged?.Invoke(this, args);
        }

        private class TextContainer : SourceTextContainer
        {
            public override event EventHandler<TextChangeEventArgs> TextChanged;

            private readonly object _outerLock;
            private SourceText _currentText;

            public TextContainer(object outerLock)
                : this(SourceText.From(string.Empty))
            {
                _outerLock = outerLock;
            }

            public TextContainer(SourceText sourceText)
            {
                if (sourceText == null)
                {
                    throw new ArgumentNullException(nameof(sourceText));
                }

                _currentText = sourceText;
            }

            public override SourceText CurrentText
            {
                get
                {
                    lock (_outerLock)
                    {
                        return _currentText;
                    }
                }
            }

            public void SetText(SourceText sourceText)
            {
                if (sourceText == null)
                {
                    throw new ArgumentNullException(nameof(sourceText));
                }

                lock (_outerLock)
                {

                    var e = new TextChangeEventArgs(_currentText, sourceText);
                    _currentText = sourceText;

                    TextChanged?.Invoke(this, e);
                }
            }
        }
    }
}
