// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class ImplicitExpression
    {
        #line hidden
        public ImplicitExpression()
        {
        }

        public override async Task ExecuteAsync()
        {
#line 1 "ImplicitExpression.cshtml"
 for(int i = 1; i <= 10; i++) {

#line default
#line hidden

            WriteLiteral("    <p>This is item #");
            Write(
#line 2 "ImplicitExpression.cshtml"
                      i

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n");
#line 3 "ImplicitExpression.cshtml"
}

#line default
#line hidden

        }
    }
}
