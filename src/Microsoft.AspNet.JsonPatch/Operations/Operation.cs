// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;
using Newtonsoft.Json;

namespace Microsoft.AspNet.JsonPatch.Operations
{
    public class Operation : OperationBase
    {
        [JsonProperty("value")]
        public object value { get; set; }

        public Operation()
        {

        }

        public Operation([NotNull] string op, [NotNull] string path, string from, object value)
            : base(op, path, from)
        {
            this.value = value;
        }

        public Operation([NotNull] string op, [NotNull] string path, string from)
            : base(op, path, from)
        {

        }

    }
}