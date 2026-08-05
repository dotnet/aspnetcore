// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Text;
using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Endpoints.Forms;

internal sealed class ClientValidationDataWriter
{
    private readonly ArrayBufferWriter<byte> _buffer;
    private readonly Utf8JsonWriter _writer;

    private string? _pendingFieldName;
    private bool _fieldOpen;
    private bool _paramsOpen;
    private bool _anyField;

    public ClientValidationDataWriter()
    {
        _buffer = new ArrayBufferWriter<byte>(initialCapacity: 256);
        _writer = new Utf8JsonWriter(_buffer);
        _writer.WriteStartObject();
        _writer.WritePropertyName("fields"u8);
        _writer.WriteStartArray();
    }

    // Records the field name. The field object is only written once its first rule arrives, so a
    // field with no rules produces no output.
    public void BeginField(string name)
    {
        _pendingFieldName = name;
        _fieldOpen = false;
    }

    // Starts a rule whose name is a compile-time UTF-8 literal (built-in rules).
    public void BeginRule(ReadOnlySpan<byte> nameUtf8, string message)
    {
        EnsureFieldOpen();
        _writer.WriteStartObject();
        _writer.WriteString("name"u8, nameUtf8);
        _writer.WriteString("message"u8, message);
        _paramsOpen = false;
    }

    // Starts a rule whose name comes from a user-defined adapter.
    public void BeginRule(string name, string message)
    {
        EnsureFieldOpen();
        _writer.WriteStartObject();
        _writer.WriteString("name"u8, name);
        _writer.WriteString("message"u8, message);
        _paramsOpen = false;
    }

    public void Param(ReadOnlySpan<byte> keyUtf8, string value)
    {
        EnsureParamsOpen();
        _writer.WriteString(keyUtf8, value);
    }

    // Writes a numeric parameter as a string (all params are strings on the wire) without
    // allocating an intermediate string.
    public void Param(ReadOnlySpan<byte> keyUtf8, int value)
    {
        EnsureParamsOpen();
        Span<byte> utf8Value = stackalloc byte[16];
        Utf8Formatter.TryFormat(value, utf8Value, out var bytesWritten);
        _writer.WriteString(keyUtf8, utf8Value[..bytesWritten]);
    }

    public void Param(string key, string value)
    {
        EnsureParamsOpen();
        _writer.WriteString(key, value);
    }

    public void EndRule()
    {
        if (_paramsOpen)
        {
            _writer.WriteEndObject(); // params
            _paramsOpen = false;
        }

        _writer.WriteEndObject(); // rule
    }

    public void EndField()
    {
        if (_fieldOpen)
        {
            _writer.WriteEndArray(); // rules
            _writer.WriteEndObject(); // field
            _fieldOpen = false;
        }

        _pendingFieldName = null;
    }

    // Finishes the document. Returns null when no field produced any rule, so the caller can skip
    // emitting the carrier element entirely.
    public string? Complete()
    {
        _writer.WriteEndArray(); // fields
        _writer.WriteEndObject(); // root
        _writer.Flush();

        // Utf8JsonWriter's default encoder escapes <, >, &, ' - safe for HTML attribute/text content.
        var result = _anyField ? Encoding.UTF8.GetString(_buffer.WrittenSpan) : null;
        _writer.Dispose();
        return result;
    }

    private void EnsureFieldOpen()
    {
        if (!_fieldOpen)
        {
            _writer.WriteStartObject();
            _writer.WriteString("name"u8, _pendingFieldName!);
            _writer.WritePropertyName("rules"u8);
            _writer.WriteStartArray();
            _fieldOpen = true;
            _anyField = true;
        }
    }

    private void EnsureParamsOpen()
    {
        if (!_paramsOpen)
        {
            _writer.WritePropertyName("params"u8);
            _writer.WriteStartObject();
            _paramsOpen = true;
        }
    }
}
