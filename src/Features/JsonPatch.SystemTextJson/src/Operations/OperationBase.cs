// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations;

public class OperationBase
{
	private string _op;
	private OperationType _operationType;

	[System.Text.Json.Serialization.JsonIgnore]
	public OperationType OperationType
	{
		get
		{
			return _operationType;
		}
	}

	[System.Text.Json.Serialization.JsonPropertyName(nameof(path))]
	public string path { get; set; }

	[System.Text.Json.Serialization.JsonPropertyName(nameof(op))]
	public string op
	{
		get
		{
			return _op;
		}
		set
		{
			OperationType result;
			if (!System.Enum.TryParse(value, ignoreCase: true, result: out result))
			{
				result = OperationType.Invalid;
			}
			_operationType = result;
			_op = value;
		}
	}

	[System.Text.Json.Serialization.JsonPropertyName(nameof(from))]
	[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
	public string from { get; set; }

	public OperationBase()
	{
	}

	public OperationBase(string op, string path, string from)
	{
		Microsoft.AspNetCore.Shared.ArgumentNullThrowHelper.ThrowIfNull(op);
		Microsoft.AspNetCore.Shared.ArgumentNullThrowHelper.ThrowIfNull(path);

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
