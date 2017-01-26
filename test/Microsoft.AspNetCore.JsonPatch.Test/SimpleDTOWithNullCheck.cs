// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.JsonPatch
{
    public class SimpleDTOWithNullCheck
    {
        private string stringProperty;

        public string StringProperty
        {
            get
            {
                return stringProperty;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }

                stringProperty = value;
            }
        }
    }
}
