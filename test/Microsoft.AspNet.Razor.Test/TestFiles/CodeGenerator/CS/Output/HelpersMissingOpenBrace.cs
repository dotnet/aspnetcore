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

    public class HelpersMissingOpenBrace
    {
public static Template 
#line 1 "HelpersMissingOpenBrace.cshtml"
Bold(string s) {

#line default
#line hidden
        return new Template((__razor_helper_writer) => {
#line 1 "HelpersMissingOpenBrace.cshtml"
                        
    s = s.ToUpper();

#line default
#line hidden

            WriteLiteralTo(__razor_helper_writer, "    <strong>");
            WriteTo(__razor_helper_writer, 
#line 3 "HelpersMissingOpenBrace.cshtml"
             s

#line default
#line hidden
            );

            WriteLiteralTo(__razor_helper_writer, "</strong>\r\n");
#line 4 "HelpersMissingOpenBrace.cshtml"

#line default
#line hidden

        }
        );
#line 4 "HelpersMissingOpenBrace.cshtml"
}

#line default
#line hidden

public static Template 
#line 6 "HelpersMissingOpenBrace.cshtml"
Italic(string s) 

#line default
#line hidden

        #line hidden
        public HelpersMissingOpenBrace()
        {
        }

        public override async Task ExecuteAsync()
        {
            WriteLiteral("\r\n");
            Write(
#line 7 "HelpersMissingOpenBrace.cshtml"
 Italic(s)

#line default
#line hidden
            );

        }
    }
}
