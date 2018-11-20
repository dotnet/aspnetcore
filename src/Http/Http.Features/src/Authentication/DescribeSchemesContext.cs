// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http.Features.Authentication
{
    public class DescribeSchemesContext
    {
        private List<IDictionary<string, object>> _results;

        public DescribeSchemesContext()
        {
            _results = new List<IDictionary<string, object>>();
        }

        public IEnumerable<IDictionary<string, object>> Results
        {
            get { return _results; }
        }

        public void Accept(IDictionary<string, object> description)
        {
            _results.Add(description);
        }
    }
}