// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNet.HttpFeature.Security;

namespace Microsoft.AspNet.PipelineCore.Security
{
    public class SignInContext : ISignInContext
    {
        private List<string> _accepted;

        public SignInContext([NotNull] IEnumerable<ClaimsIdentity> identities, IDictionary<string, string> dictionary)
        {
            Identities = identities;
            Properties = dictionary ?? new Dictionary<string, string>(StringComparer.Ordinal);
            _accepted = new List<string>();
        }

        public IEnumerable<ClaimsIdentity> Identities { get; private set; }

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
