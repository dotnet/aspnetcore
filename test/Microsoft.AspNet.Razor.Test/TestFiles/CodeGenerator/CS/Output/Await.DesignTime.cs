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

    public class Await
    {
        private static object @__o;
#line 1 "Await.cshtml"

    public async Task<string> Foo()
    {
        return "Bar";
    }

#line default
#line hidden
        private void @__RazorDesignTimeHelpers__()
        {
            #pragma warning disable 219
            #pragma warning restore 219
        }
        #line hidden
        public Await()
        {
        }

        public override async Task ExecuteAsync()
        {
#line 1 "------------------------------------------"
								 __o = await Foo();

#line default
#line hidden
#line 1 "------------------------------------------"
								__o = await Foo();

#line default
#line hidden
#line 12 "Await.cshtml"
									    await Foo(); 

#line default
#line hidden

#line 13 "Await.cshtml"
											   

#line default
#line hidden

#line 1 "------------------------------------------"
											 __o = await Foo();

#line default
#line hidden
#line 13 "Await.cshtml"
																   

#line default
#line hidden

#line 1 "------------------------------------------"
										   __o = await;

#line default
#line hidden
#line 1 "------------------------------------------"
									__o = await Foo(1, 2);

#line default
#line hidden
#line 1 "------------------------------------------"
								   __o = await Foo("bob", true);

#line default
#line hidden
#line 21 "Await.cshtml"
										   await Foo(something, hello: "world"); 

#line default
#line hidden

#line 22 "Await.cshtml"
												  

#line default
#line hidden

#line 1 "------------------------------------------"
												__o = await Foo(boolValue: false);

#line default
#line hidden
#line 22 "Await.cshtml"
																					  

#line default
#line hidden

#line 1 "------------------------------------------"
											  __o = await ("wrrronggg");

#line default
#line hidden
        }
    }
}
