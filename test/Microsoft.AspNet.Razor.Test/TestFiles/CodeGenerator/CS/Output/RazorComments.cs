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

    public class RazorComments
    {
        #line hidden
        public RazorComments()
        {
        }

        public override async Task ExecuteAsync()
        {
            WriteLiteral("\r\n<p>This should  be shown</p>\r\n\r\n");
#line 4 "RazorComments.cshtml"
  
    

#line default
#line hidden

#line 5 "RazorComments.cshtml"
                                       
    Exception foo = 

#line default
#line hidden

#line 6 "RazorComments.cshtml"
                                                  null;
    if(foo != null) {
        throw foo;
    }

#line default
#line hidden

            WriteLiteral("\r\n\r\n");
#line 12 "RazorComments.cshtml"
   var bar = "@* bar *@"; 

#line default
#line hidden

            WriteLiteral("\r\n<p>But this should show the comment syntax: ");
            Write(
#line 13 "RazorComments.cshtml"
                                             bar

#line default
#line hidden
            );

            WriteLiteral("</p>\r\n\r\n");
            Write(
#line 15 "RazorComments.cshtml"
  a

#line default
#line hidden
#line 15 "RazorComments.cshtml"
       b

#line default
#line hidden
            );

            WriteLiteral("\r\n");
        }
    }
}
