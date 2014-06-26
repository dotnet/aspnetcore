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
        private List<AuthenticationDescription> _results;

        public AuthTypeContext()
        {
            _results = new List<AuthenticationDescription>();
        }

        public IEnumerable<AuthenticationDescription> Results
        {
            get { return _results; }
        }
                
        public void Accept(IDictionary<string, object> description)
        {
            _results.Add(new AuthenticationDescription(description));
        }
    }
}
