// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations;

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

    [JsonPropertyName(nameof(path))]
    public string path { get; set; }

    [JsonPropertyName(nameof(op))]
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

    [JsonPropertyName(nameof(from))]
    public string from { get; set; }

    public OperationBase()
    {
    }

    public OperationBase(string op, string path, string from)
    {
        ArgumentNullThrowHelper.ThrowIfNull(op);
        ArgumentNullThrowHelper.ThrowIfNull(path);

        this.op = op;
        this.path = path;
        this.from = from;
    }

    public bool ShouldSerializeFrom()
    {
        return (OperationType == OperationType.Move
            || OperationType == OperationType.Copy);
    }
}
