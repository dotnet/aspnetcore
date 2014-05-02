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

    public class ExpressionsInCode
    {
        #line hidden
        public ExpressionsInCode()
        {
        }

        public override async Task ExecuteAsync()
        {
#line 1 "ExpressionsInCode.cshtml"
  
    object foo = null;
    string bar = "Foo";

#line default
#line hidden

            WriteLiteral("\r\n\r\n");
#line 6 "ExpressionsInCode.cshtml"
 if(foo != null) {
    

#line default
#line hidden

            Write(
#line 7 "ExpressionsInCode.cshtml"
     foo

#line default
#line hidden
            );

#line 7 "ExpressionsInCode.cshtml"
        
} else {

#line default
#line hidden

            WriteLiteral("    <p>Foo is Null!</p>\r\n");
#line 10 "ExpressionsInCode.cshtml"
}

#line default
#line hidden

            WriteLiteral("\r\n<p>\r\n");
#line 13 "ExpressionsInCode.cshtml"
 if(!String.IsNullOrEmpty(bar)) {
    

#line default
#line hidden

            Write(
#line 14 "ExpressionsInCode.cshtml"
      bar.Replace("F", "B")

#line default
#line hidden
            );

#line 14 "ExpressionsInCode.cshtml"
                            
}

#line default
#line hidden

            WriteLiteral("</p>");
        }
    }
}
