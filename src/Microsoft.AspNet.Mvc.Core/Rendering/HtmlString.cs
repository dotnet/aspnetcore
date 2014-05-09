// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class HtmlString
    {
        private static readonly HtmlString _empty = new HtmlString(string.Empty);

        private readonly string _input;

        public HtmlString(string input)
        {
            _input = input;
        }

        public static HtmlString Empty
        {
            get
            {
                return _empty;
            }
        }

        public override string ToString()
        {
            return _input;
        }
    }
}
