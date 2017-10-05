// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class MatcherCollection : Collection<MatcherEntry>
    {
        public void Add(MatcherBase matcher)
        {
            if (matcher == null)
            {
                throw new ArgumentNullException(nameof(matcher));
            }

            Add(new MatcherEntry()
            {
                Matcher = matcher,
                AddressProvider  = matcher,
                EndpointProvider = matcher,
            });
        }

        public void Add(IMatcher matcher)
        {
            if (matcher == null)
            {
                throw new ArgumentNullException(nameof(matcher));
            }

            Add(new MatcherEntry()
            {
                Matcher = matcher,
            });
        }
    }
}
