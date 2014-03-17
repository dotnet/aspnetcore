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
