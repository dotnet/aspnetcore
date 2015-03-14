// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Diagnostics.Elm
{
    public class ElmLoggerProvider : ILoggerProvider
    {
        private readonly ElmStore _store;
        private readonly ElmOptions _options;

        public ElmLoggerProvider([NotNull] ElmStore store, [NotNull] ElmOptions options)
        {
            _store = store;
            _options = options;
        }

        public ILogger CreateLogger(string name)
        {
            return new ElmLogger(name, _options, _store);
        }
    }
}
