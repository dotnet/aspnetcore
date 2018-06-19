// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing.Template;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    internal static class RouteTemplateWriter
    {
        public static string ToString(IEnumerable<TemplateSegment> routeSegments)
        {
            return string.Join("/", routeSegments.Select(s => ToString(s)));
        }

        private static string ToString(TemplateSegment templateSegment)
        {
            return string.Join(string.Empty, templateSegment.Parts.Select(p => ToString(p)));
        }

        private static string ToString(TemplatePart templatePart)
        {
            if (templatePart.IsParameter)
            {
                var partText = "{";
                if (templatePart.IsCatchAll)
                {
                    partText += "*";
                }
                partText += templatePart.Name;
                foreach (var item in templatePart.InlineConstraints)
                {
                    partText += ":";
                    partText += item.Constraint;
                }
                if (templatePart.DefaultValue != null)
                {
                    partText += "=";
                    partText += templatePart.DefaultValue;
                }
                if (templatePart.IsOptional)
                {
                    partText += "?";
                }
                partText += "}";

                return partText;
            }
            else
            {
                return templatePart.Text;
            }
        }
    }
}