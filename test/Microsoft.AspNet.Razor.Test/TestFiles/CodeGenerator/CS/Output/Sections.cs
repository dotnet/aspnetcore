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

    public class Sections
    {
        #line hidden
        public Sections()
        {
        }

        public override async Task ExecuteAsync()
        {
#line 1 "Sections.cshtml"
  
    Layout = "_SectionTestLayout.cshtml"

#line default
#line hidden

            WriteLiteral("\r\n\r\n<div>This is in the Body>\r\n\r\n");
            DefineSection("Section2", new Template((__razor_template_writer) => {
                WriteLiteralTo(__razor_template_writer, "\r\n    <div>This is in Section 2</div>\r\n");
            }
            ));
            WriteLiteral("\r\n");
            DefineSection("Section1", new Template((__razor_template_writer) => {
                WriteLiteralTo(__razor_template_writer, "\r\n    <div>This is in Section 1</div>\r\n");
            }
            ));
        }
    }
}
