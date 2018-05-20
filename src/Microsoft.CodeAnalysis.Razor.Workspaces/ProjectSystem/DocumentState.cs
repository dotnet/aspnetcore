// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class DocumentState
    {
        private static readonly TextAndVersion EmptyText = TextAndVersion.Create(
            SourceText.From(string.Empty),
            VersionStamp.Default);

        public static readonly Func<Task<TextAndVersion>> EmptyLoader = () => Task.FromResult(EmptyText);

        private readonly object _lock;

        private Func<Task<TextAndVersion>> _loader;
        private Task<TextAndVersion> _loaderTask;
        private SourceText _sourceText;
        private VersionStamp? _version;

        private DocumentGeneratedOutputTracker _generatedOutput;
        private DocumentImportsTracker _imports;

        public static DocumentState Create(
            HostWorkspaceServices services,
            HostDocument hostDocument,
            Func<Task<TextAndVersion>> loader)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (hostDocument == null)
            {
                throw new ArgumentNullException(nameof(hostDocument));
            }

            loader = loader ?? EmptyLoader;
            return new DocumentState(services, hostDocument, null, null, loader);
        }

        // Internal for testing
        internal DocumentState(
            HostWorkspaceServices services,
            HostDocument hostDocument,
            SourceText text,
            VersionStamp? version,
            Func<Task<TextAndVersion>> loader)
        {
            Services = services;
            HostDocument = hostDocument;
            _sourceText = text;
            _version = version;
            _loader = loader;
            _lock = new object();
        }

        public HostDocument HostDocument { get; }

        public HostWorkspaceServices Services { get; }

        public GeneratedCodeContainer GeneratedCodeContainer => HostDocument.GeneratedCodeContainer;

        public DocumentGeneratedOutputTracker GeneratedOutput
        {
            get
            {
                if (_generatedOutput == null)
                {
                    lock (_lock)
                    {
                        if (_generatedOutput == null)
                        {
                            _generatedOutput = new DocumentGeneratedOutputTracker(null);
                        }
                    }
                }

                return _generatedOutput;
            }
        }

        public DocumentImportsTracker Imports
        {
            get
            {
                if (_imports == null)
                {
                    lock (_lock)
                    {
                        if (_imports == null)
                        {
                            _imports = new DocumentImportsTracker();
                        }
                    }
                }

                return _imports;
            }
        }

        public async Task<SourceText> GetTextAsync()
        {
            if (TryGetText(out var text))
            {
                return text;
            }

            lock (_lock)
            {
                _loaderTask = _loader();
            }

            return (await _loaderTask.ConfigureAwait(false)).Text;
        }

        public async Task<VersionStamp> GetTextVersionAsync()
        {
            if (TryGetTextVersion(out var version))
            {
                return version;
            }

            lock (_lock)
            {
                _loaderTask = _loader();
            }

            return (await _loaderTask.ConfigureAwait(false)).Version;
        }

        public bool TryGetText(out SourceText result)
        {
            if (_sourceText != null)
            {
                result = _sourceText;
                return true;
            }

            if (_loaderTask != null && _loaderTask.IsCompleted)
            {
                result = _loaderTask.Result.Text;
                return true;
            }

            result = null;
            return false;
        }

        public bool TryGetTextVersion(out VersionStamp result)
        {
            if (_version != null)
            {
                result = _version.Value;
                return true;
            }

            if (_loaderTask != null && _loaderTask.IsCompleted)
            {
                result = _loaderTask.Result.Version;
                return true;
            }

            result = default;
            return false;
        }

        public virtual DocumentState WithConfigurationChange()
        {
            var state = new DocumentState(Services, HostDocument, _sourceText, _version, _loader);

            // The source could not have possibly changed.
            state._sourceText = _sourceText;
            state._version = _version;
            state._loaderTask = _loaderTask;

            return state;
        }

        public virtual DocumentState WithWorkspaceProjectChange()
        {
            var state = new DocumentState(Services, HostDocument, _sourceText, _version, _loader);

            // The source could not have possibly changed.
            state._sourceText = _sourceText;
            state._version = _version;
            state._loaderTask = _loaderTask;

            // Opportunistically cache the generated code
            state._generatedOutput = _generatedOutput?.Fork();

            return state;
        }

        public virtual DocumentState WithText(SourceText sourceText, VersionStamp version)
        {
            if (sourceText == null)
            {
                throw new ArgumentNullException(nameof(sourceText));
            }

            return new DocumentState(Services, HostDocument, sourceText, version, null);
        }

        public virtual DocumentState WithTextLoader(Func<Task<TextAndVersion>> loader)
        {
            if (loader == null)
            {
                throw new ArgumentNullException(nameof(loader));
            }

            return new DocumentState(Services, HostDocument, null, null, loader);
        }
    }
}
