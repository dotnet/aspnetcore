// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Razor.Parser
{
    public static class ParserVisitorExtensions
    {
        public static void Visit(this ParserVisitor self, ParserResults result)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            result.Document.Accept(self);
            foreach (RazorError error in result.ParserErrors)
            {
                self.VisitError(error);
            }
            self.OnComplete();
        }
    }
}
