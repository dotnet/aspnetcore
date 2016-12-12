// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Tools.Internal
{
    public class ConditionalFormatter : IFormatter
    {
        private readonly Func<bool> _predicate;

        public ConditionalFormatter(Func<bool> predicate)
        {
            Ensure.NotNull(predicate, nameof(predicate));

            _predicate = predicate;
        }

        public string Format(string text)
            => _predicate()
                ? text
                : null;
    }
}