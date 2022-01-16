// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.JsonPatch.Operations;

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
