// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Http.Interfaces.Authentication;

namespace Microsoft.AspNet.Http.Core.Authentication
{
    public class DescribeSchemesContext : IDescribeSchemesContext
    {
        private List<AuthenticationDescription> _results;

        public DescribeSchemesContext()
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
