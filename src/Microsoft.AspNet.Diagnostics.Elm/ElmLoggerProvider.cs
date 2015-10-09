// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Diagnostics.Elm
{
    public class ElmLoggerProvider : ILoggerProvider
    {
        private readonly ElmStore _store;
        private readonly ElmOptions _options;

        public ElmLoggerProvider(ElmStore store, ElmOptions options)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _store = store;
            _options = options;
        }

        public ILogger CreateLogger(string name)
        {
            return new ElmLogger(name, _options, _store);
        }

        public void Dispose()
        {
        }
    }
}
