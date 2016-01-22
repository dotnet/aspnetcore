// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.JsonPatch.Test.Dynamic
{
    public class SimpleDTOWithNestedDTO
    {
        public int IntegerValue { get; set; }
        public NestedDTO NestedDTO { get; set; }
        public SimpleDTO SimpleDTO { get; set; }
        public List<SimpleDTO> ListOfSimpleDTO { get; set; }

        public SimpleDTOWithNestedDTO()
        {
            NestedDTO = new NestedDTO();
            SimpleDTO = new SimpleDTO();
            ListOfSimpleDTO = new List<SimpleDTO>();
        }
    }
}
