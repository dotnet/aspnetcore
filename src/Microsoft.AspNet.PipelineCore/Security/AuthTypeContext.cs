// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http.Security;
using Microsoft.AspNet.HttpFeature.Security;

namespace Microsoft.AspNet.PipelineCore.Security
{
    public class AuthTypeContext : IAuthTypeContext
    {
        public AuthTypeContext()
        {
            Results = new List<AuthenticationDescription>();
        }

        public IList<AuthenticationDescription> Results { get; private set; }
                
        public void Accept(IDictionary<string, object> description)
        {
            Results.Add(new AuthenticationDescription(description));
        }
    }
}
