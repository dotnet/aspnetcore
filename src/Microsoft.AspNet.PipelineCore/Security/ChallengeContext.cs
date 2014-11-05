// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.HttpFeature.Security;

namespace Microsoft.AspNet.PipelineCore.Security
{
    public class ChallengeContext : IChallengeContext
    {
        private List<string> _accepted;

        public ChallengeContext([NotNull] IEnumerable<string> authenticationTypes, IDictionary<string, string> properties)
        {
            AuthenticationTypes = authenticationTypes;
            Properties = properties ?? new Dictionary<string, string>(StringComparer.Ordinal);
            _accepted = new List<string>();
        }

        public IEnumerable<string> AuthenticationTypes { get; private set; }

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
