// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.HttpFeature.Security;

namespace Microsoft.AspNet.PipelineCore.Security
{
    public class SignOutContext : ISignOutContext
    {
        public SignOutContext(IList<string> authenticationTypes)
        {
            if (authenticationTypes == null)
            {
                throw new ArgumentNullException("authenticationTypes");
            }
            AuthenticationTypes = authenticationTypes;
            Accepted = new List<string>();
        }

        public IList<string> AuthenticationTypes { get; private set; }

        public IList<string> Accepted { get; private set; }

        public void Accept(string authenticationType, IDictionary<string, object> description)
        {
            Accepted.Add(authenticationType);
        }
    }
}
