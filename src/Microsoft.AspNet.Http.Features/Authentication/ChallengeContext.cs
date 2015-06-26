// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Http.Features.Authentication
{
    public class ChallengeContext
    {
        public ChallengeContext([NotNull] string authenticationScheme) : this(authenticationScheme, properties: null, behavior: ChallengeBehavior.Automatic)
        {
        }

        public ChallengeContext([NotNull] string authenticationScheme, IDictionary<string, string> properties, ChallengeBehavior behavior)
        {
            AuthenticationScheme = authenticationScheme;
            Properties = properties ?? new Dictionary<string, string>(StringComparer.Ordinal);
            Behavior = behavior;
        }

        public string AuthenticationScheme { get; }

        public ChallengeBehavior Behavior { get; }

        public IDictionary<string, string> Properties { get; }

        public bool Accepted { get; private set; }

        public void Accept()
        {
            Accepted = true;
        }
    }
}