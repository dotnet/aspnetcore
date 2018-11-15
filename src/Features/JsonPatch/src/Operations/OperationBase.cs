// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.JsonPatch.Operations
{
    public class OperationBase
    {
        private string _op;
        private OperationType _operationType;

        [JsonIgnore]
        public OperationType OperationType
        {
            get
            {
                return _operationType;
            }
        }

        [JsonProperty("path")]
        public string path { get; set; }

        [JsonProperty("op")]
        public string op
        {
            get
            {
                return _op;
            }
            set
            {
                OperationType result;
                if (!Enum.TryParse(value, ignoreCase: true, result: out result))
                {
                    result = OperationType.Invalid;
                }
                _operationType = result;
                _op = value;
            }
        }

        [JsonProperty("from")]
        public string from { get; set; }

        public OperationBase()
        {

        }

        public OperationBase(string op, string path, string from)
        {
            if (op == null)
            {
                throw new ArgumentNullException(nameof(op));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            this.op = op;
            this.path = path;
            this.from = from;
        }

        public bool ShouldSerializefrom()
        {
            return (OperationType == OperationType.Move
                || OperationType == OperationType.Copy);
        }
    }
}