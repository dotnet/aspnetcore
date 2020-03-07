// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.Rewrite.IISUrlRewrite
{
    internal static class RewriteMapParser
    {
        public static IISRewriteMapCollection Parse(XElement xmlRoot)
        {
            if (xmlRoot == null)
            {
                throw new ArgumentNullException(nameof(xmlRoot));
            }

            var mapsElement = xmlRoot.Descendants(RewriteTags.RewriteMaps).SingleOrDefault();
            if (mapsElement == null)
            {
                return null;
            }

            var rewriteMaps = new IISRewriteMapCollection();
            foreach (var mapElement in mapsElement.Elements(RewriteTags.RewriteMap))
            {
                var map = new IISRewriteMap(mapElement.Attribute(RewriteTags.Name)?.Value);
                foreach (var addElement in mapElement.Elements(RewriteTags.Add))
                {
                    map[addElement.Attribute(RewriteTags.Key).Value.ToLowerInvariant()] = addElement.Attribute(RewriteTags.Value).Value;
                }
                rewriteMaps.Add(map);
            }

            return rewriteMaps;
        }
    }
}