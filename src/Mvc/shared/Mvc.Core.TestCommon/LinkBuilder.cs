// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc
{
    public class LinkBuilder
    {
        public LinkBuilder(string url)
        {
            Url = url;

            Values = new Dictionary<string, object>
            {
                { "link", string.Empty }
            };
        }

        public string Url { get; set; }

        public Dictionary<string, object> Values { get; set; }

        public LinkBuilder To(object values)
        {
            var dictionary = new RouteValueDictionary(values);
            foreach (var kvp in dictionary)
            {
                Values.Add("link_" + kvp.Key, kvp.Value);
            }

            return this;
        }

        public override string ToString()
        {
            return Url + "?" + string.Join("&", Values.Select(kvp => kvp.Key + "=" + kvp.Value));
        }

        public static implicit operator string(LinkBuilder builder)
        {
            return builder.ToString();
        }
    }
}