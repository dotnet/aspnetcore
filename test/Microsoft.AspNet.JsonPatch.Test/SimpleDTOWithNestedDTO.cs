// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.JsonPatch.Test
{

    public class SimpleDTOWithNestedDTO
    {
        public int IntegerValue { get; set; }

        public NestedDTO NestedDTO { get; set; }

        public SimpleDTO SimpleDTO { get; set; }

        public SimpleDTOWithNestedDTO()
        {
            this.NestedDTO = new NestedDTO();
            this.SimpleDTO = new SimpleDTO();
        }
    }
}