using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public class AccessibilityModificatorDirectivesPass: IntermediateNodePassBase, IRazorDirectiveClassifierPass
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var @class = documentNode.FindPrimaryClass();
            if (@class == null)
            {
                return;
            }

            string modificator = null;

            // find accessibility modificator directives in documentNode
            if (documentNode.FindDirectiveReferences(AccessibilityModificatorDirectives.PublicDirective).Count > 0)
            {
                modificator = "public";
            }

            if (documentNode.FindDirectiveReferences(AccessibilityModificatorDirectives.InternalDirective).Count > 0)
            {
                modificator = "internal";
            }

            if (documentNode.FindDirectiveReferences(AccessibilityModificatorDirectives.PrivateDirective).Count > 0)
            {
                modificator = "private";
            }

            // if of the accessibility modificator directives founded remove previous (usually public) and add new
            if (modificator != null)
            {
                @class.Modifiers.Remove("public");
                @class.Modifiers.Remove("private");
                @class.Modifiers.Remove("internal");
                @class.Modifiers.Insert(0, modificator);
            }
        }
    }
}
