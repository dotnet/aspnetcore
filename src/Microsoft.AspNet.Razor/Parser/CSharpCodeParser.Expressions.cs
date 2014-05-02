// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Parser
{
    public partial class CSharpCodeParser
    {
        private void SetUpExpressions()
        {
            MapKeywords(AwaitExpression, CSharpKeyword.Await);
        }

        private void AwaitExpression(bool topLevel)
        {
            // Ensure that we're on the await statement (only runs in debug)
            Assert(CSharpKeyword.Await);

            // Accept the "await" and move on
            AcceptAndMoveNext();
            
            // Accept 1 or more spaces between the await and the following code.
            AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));

            // Accept a single code piece to await. This will accept up until a method "call" signature.
            // Ex: "@await |Foo|()" Inbetween the pipes is what is accepted.  The Statement/ImplicitExpression
            // handling capture method calls and the parameters passed in.
            AcceptWhile(CSharpSymbolType.Identifier);

            // Top level basically indicates if we're within an expression or statement.
            // Ex: topLevel true = @await Foo()  |  topLevel false = @{ await Foo(); }
            // Note that in this case @{ <b>@await Foo()</b> } top level is true for await.
            // Therefore, if we're top level then we want to act like an implicit expression,
            // otherwise just act as whatever we're contained in.
            if (topLevel)
            {
                // Setup the Span to be an async implicit expression (an implicit expresison that allows spaces).
                // Spaces are allowed because of "@await Foo()".
                AsyncImplicitExpression();
            }
        }
    }
}
