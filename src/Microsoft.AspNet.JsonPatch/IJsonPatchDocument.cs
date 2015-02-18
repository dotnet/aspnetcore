// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.JsonPatch.Operations;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNet.JsonPatch
{
    public interface IJsonPatchDocument
    {
        IContractResolver ContractResolver { get; set; }

        List<Operation> GetOperations();
    }
}