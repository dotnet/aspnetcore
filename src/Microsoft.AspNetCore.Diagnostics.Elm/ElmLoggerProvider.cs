// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.Elm
{
    public class ElmLoggerProvider : ILoggerProvider
    {
        private readonly ElmStore _store;
        private readonly ElmOptions _options;

        public ElmLoggerProvider(ElmStore store, IOptions<ElmOptions> options)
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
            _options = options.Value;
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
