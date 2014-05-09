// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Parser
{
    public static class ParserVisitorExtensions
    {
        public static void Visit(this ParserVisitor self, ParserResults result)
        {
            if (self == null)
            {
                throw new ArgumentNullException("self");
            }
            if (result == null)
            {
                throw new ArgumentNullException("result");
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
