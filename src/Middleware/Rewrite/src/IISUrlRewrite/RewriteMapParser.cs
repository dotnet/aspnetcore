// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.Rewrite.IISUrlRewrite;

internal static class RewriteMapParser
{
    public static IISRewriteMapCollection? Parse(XElement xmlRoot)
    {
        ArgumentNullException.ThrowIfNull(xmlRoot);

        var mapsElement = xmlRoot.Descendants(RewriteTags.RewriteMaps).SingleOrDefault();
        if (mapsElement == null)
        {
            return null;
        }

        var rewriteMaps = new IISRewriteMapCollection();
        foreach (var mapElement in mapsElement.Elements(RewriteTags.RewriteMap))
        {
            var map = new IISRewriteMap(mapElement.Attribute(RewriteTags.Name)?.Value!);
            foreach (var addElement in mapElement.Elements(RewriteTags.Add))
            {
                map[addElement.Attribute(RewriteTags.Key)!.Value.ToLowerInvariant()] = addElement.Attribute(RewriteTags.Value)!.Value;
            }
            rewriteMaps.Add(map);
        }

        return rewriteMaps;
    }
}
