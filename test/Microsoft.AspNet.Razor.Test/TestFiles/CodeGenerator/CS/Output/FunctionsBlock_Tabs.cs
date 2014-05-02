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

    public class FunctionsBlock_Tabs
    {
#line 1 "FunctionsBlock_Tabs.cshtml"



#line default
#line hidden
#line 5 "FunctionsBlock_Tabs.cshtml"

	Random _rand = new Random();
	private int RandomInt() {
		return _rand.Next();
	}

#line default
#line hidden
        #line hidden
        public FunctionsBlock_Tabs()
        {
        }

        public override async Task ExecuteAsync()
        {
            WriteLiteral("\r\n");
            WriteLiteral("\r\nHere\'s a random number: ");
            Write(
#line 12 "FunctionsBlock_Tabs.cshtml"
                         RandomInt()

#line default
#line hidden
            );

        }
    }
}
