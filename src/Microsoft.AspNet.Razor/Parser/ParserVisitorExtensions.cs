// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Parser
{
    public static class ParserVisitorExtensions
    {
        public static void Visit([NotNull] this ParserVisitor self, [NotNull] ParserResults result)
        {
            result.Document.Accept(self);
            foreach (RazorError error in result.ParserErrors)
            {
                self.VisitError(error);
            }
            self.OnComplete();
        }
    }
}
