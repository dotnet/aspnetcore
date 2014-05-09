// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class ExplicitExpression
    {
        #line hidden
        public ExplicitExpression()
        {
        }

        public override async Task ExecuteAsync()
        {
            WriteLiteral("1 + 1 = ");
            Write(
#line 1 "ExplicitExpression.cshtml"
          1+1

#line default
#line hidden
            );

        }
    }
}
