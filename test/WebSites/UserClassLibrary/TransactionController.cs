// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace UserClassLibrary
{
    // A type with the suffix Controller that lives in an assembly that does not reference Mvc.
    // This will not be treated as a controller in an Mvc application.
    public class TransactionController
    {
        public int UpdateTransaction()
        {
            return 1;
        }
    }
}
