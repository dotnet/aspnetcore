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

    public class Templates
    {
#line 1 "Templates.cshtml"

    public HelperResult Repeat(int times, Func<int, object> template) {
        return new HelperResult((writer) => {
            for(int i = 0; i < times; i++) {
                ((HelperResult)template(i)).WriteTo(writer);
            }
        });
    }

#line default
#line hidden
        #line hidden
        public Templates()
        {
        }

        public override async Task ExecuteAsync()
        {
            WriteLiteral("\r\n");
#line 11 "Templates.cshtml"
  
    Func<dynamic, object> foo = 

#line default
#line hidden

            item => new Template((__razor_template_writer) => {
                WriteLiteralTo(__razor_template_writer, "This works ");
                WriteTo(__razor_template_writer, 
#line 12 "Templates.cshtml"
                                                   item

#line default
#line hidden
                );

                WriteLiteralTo(__razor_template_writer, "!");
            }
            )
#line 12 "Templates.cshtml"
                                                               ;
    

#line default
#line hidden

            Write(
#line 13 "Templates.cshtml"
     foo("")

#line default
#line hidden
            );

#line 13 "Templates.cshtml"
            

#line default
#line hidden

            WriteLiteral("\r\n\r\n<ul>\r\n");
            Write(
#line 17 "Templates.cshtml"
  Repeat(10, 

#line default
#line hidden
            item => new Template((__razor_template_writer) => {
                WriteLiteralTo(__razor_template_writer, "<li>Item #");
                WriteTo(__razor_template_writer, 
#line 17 "Templates.cshtml"
                         item

#line default
#line hidden
                );

                WriteLiteralTo(__razor_template_writer, "</li>");
            }
            )
#line 17 "Templates.cshtml"
                                  )

#line default
#line hidden
            );

            WriteLiteral("\r\n</ul>\r\n\r\n<p>\r\n");
            Write(
#line 21 "Templates.cshtml"
 Repeat(10,
    

#line default
#line hidden
            item => new Template((__razor_template_writer) => {
                WriteLiteralTo(__razor_template_writer, " This is line#");
                WriteTo(__razor_template_writer, 
#line 22 "Templates.cshtml"
                     item

#line default
#line hidden
                );

                WriteLiteralTo(__razor_template_writer, " of markup<br/>\r\n");
            }
            )
#line 23 "Templates.cshtml"
)

#line default
#line hidden
            );

            WriteLiteral("\r\n</p>\r\n\r\n<ul>\r\n    ");
            Write(
#line 27 "Templates.cshtml"
     Repeat(10, 

#line default
#line hidden
            item => new Template((__razor_template_writer) => {
                WriteLiteralTo(__razor_template_writer, "<li>\r\n        Item #");
                WriteTo(__razor_template_writer, 
#line 28 "Templates.cshtml"
               item

#line default
#line hidden
                );

                WriteLiteralTo(__razor_template_writer, "\r\n");
#line 29 "Templates.cshtml"
        

#line default
#line hidden

#line 29 "Templates.cshtml"
          var parent = item;

#line default
#line hidden

                WriteLiteralTo(__razor_template_writer, "\r\n        <ul>\r\n            <li>Child Items... ?</li>\r\n            \r\n        </ul" +
">\r\n    </li>");
            }
            )
#line 34 "Templates.cshtml"
         )

#line default
#line hidden
            );

            WriteLiteral("\r\n</ul> ");
        }
    }
}
