// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.HttpFeature.Security;

namespace Microsoft.AspNet.PipelineCore.Security
{
    public class SignOutContext : ISignOutContext
    {
        private List<string> _accepted;

        public SignOutContext([NotNull] IEnumerable<string> authenticationTypes)
        {
            AuthenticationTypes = authenticationTypes;
            _accepted = new List<string>();
        }

        public IEnumerable<string> AuthenticationTypes { get; private set; }

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
