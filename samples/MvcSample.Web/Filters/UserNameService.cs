// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace MvcSample.Web.Filters
{
    public class UserNameService
    {
        private static readonly string[] _userNames = new[] { "Jon", "David", "Goliath" };
        private static int _index;

        public string GetName()
        {
            return _userNames[_index++ % 3];
        }
    }
}
