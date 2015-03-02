// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http.Interfaces.Authentication;

namespace Microsoft.AspNet.Http.Core.Authentication
{
    public class ChallengeContext : IChallengeContext
    {
        private List<string> _accepted;

        public ChallengeContext([NotNull] IEnumerable<string> authenticationSchemes, IDictionary<string, string> properties)
        {
            AuthenticationSchemes = authenticationSchemes;
            Properties = properties ?? new Dictionary<string, string>(StringComparer.Ordinal);
            _accepted = new List<string>();
        }

        public IEnumerable<string> AuthenticationSchemes { get; private set; }

        public IDictionary<string, string> Properties { get; private set; }

        public IEnumerable<string> Accepted
        {
            get { return _accepted; }
        }

        public void Accept(string authenticationType, IDictionary<string, object> description)
        {
            _accepted.Add(authenticationType);
        }
    }
}
