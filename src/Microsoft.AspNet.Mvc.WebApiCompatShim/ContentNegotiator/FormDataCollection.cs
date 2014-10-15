// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNETCORE50

using Microsoft.AspNet.WebUtilities;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace System.Net.Http.Formatting
{
    public class FormDataCollection : IEnumerable<KeyValuePair<string, string>>
    {
        private readonly IList<KeyValuePair<string, string>> _values;

        public FormDataCollection(string query)
        {
            var parsedQuery = QueryHelpers.ParseQuery(query);

            var values = new List<KeyValuePair<string, string>>();
            foreach (var kvp in parsedQuery)
            {
                foreach (var value in kvp.Value)
                {
                    values.Add(new KeyValuePair<string, string>(kvp.Key, value));
                }
            }

            _values = values;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _values.GetEnumerator();
        }
    }
}

#endif