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

namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class MarkupInCodeBlock
    {
        #line hidden
        public MarkupInCodeBlock()
        {
        }

        public override async Task ExecuteAsync()
        {
#line 1 "MarkupInCodeBlock.cshtml"
  
    for(int i = 1; i <= 10; i++) {

#line default
#line hidden

            WriteLiteral("        <p>Hello from C#, #");
            Write(
#line 3 "MarkupInCodeBlock.cshtml"
                             i.ToString()

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n");
#line 4 "MarkupInCodeBlock.cshtml"
    }

#line default
#line hidden

            WriteLiteral("\r\n");
        }
    }
}
