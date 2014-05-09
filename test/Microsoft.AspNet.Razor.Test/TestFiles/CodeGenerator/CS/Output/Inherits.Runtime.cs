// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class Inherits : foo.bar<baz<biz>>.boz bar
    {
        #line hidden
        public Inherits()
        {
        }

        public override async Task ExecuteAsync()
        {
            Write(
#line 1 "Inherits.cshtml"
 foo()

#line default
#line hidden
            );

            WriteLiteral("\r\n\r\n");
        }
    }
}
