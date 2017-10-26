// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    internal class Customer
    {
        private string _name;
        private int _age;

        public Customer(string name, int age)
        {
            _name = name;
            _age = age;
        }
    }
}
