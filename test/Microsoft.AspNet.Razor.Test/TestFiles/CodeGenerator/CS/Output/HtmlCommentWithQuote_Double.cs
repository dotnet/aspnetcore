// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace TestOutput
{
    using System;
    using System.Threading.Tasks;

    public class HtmlCommentWithQuote_Double
    {
        #line hidden
        public HtmlCommentWithQuote_Double()
        {
        }

        public override async Task ExecuteAsync()
        {
            WriteLiteral("<!-- \" -->\r\n<img");
            WriteAttribute("src", Tuple.Create(" src=\"", 16), Tuple.Create("\"", 41), Tuple.Create(Tuple.Create("", 22), Tuple.Create<System.Object, System.Int32>(Href("~/images/submit.png"), 22), false));
            WriteLiteral(" />");
        }
    }
}
