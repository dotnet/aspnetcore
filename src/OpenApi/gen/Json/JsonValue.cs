// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators.Json;

internal abstract class JsonValue
{
    public abstract void WriteTo(StringBuilder builder);

    public string ToCanonicalJson()
    {
        var builder = new StringBuilder();
        WriteTo(builder);
        return builder.ToString();
    }

    public static JsonValue Parse(string text)
        => new Parser(text).Parse();

    protected static void WriteString(StringBuilder builder, string value)
    {
        builder.Append('"');
        foreach (var character in value)
        {
            switch (character)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (character < ' ')
                    {
                        builder.Append("\\u");
                        builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(character);
                    }
                    break;
            }
        }
        builder.Append('"');
    }

    private sealed class Parser
    {
        private readonly string _text;
        private int _position;

        public Parser(string text)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
        }

        public JsonValue Parse()
        {
            SkipWhiteSpace();
            var value = ParseValue();
            SkipWhiteSpace();
            if (_position != _text.Length)
            {
                throw Error("Unexpected trailing content.");
            }
            return value;
        }

        private JsonValue ParseValue()
        {
            if (_position == _text.Length)
            {
                throw Error("Expected a JSON value.");
            }

            return _text[_position] switch
            {
                '{' => ParseObject(),
                '[' => ParseArray(),
                '"' => new JsonString(ParseString()),
                't' => ParseLiteral("true", JsonBoolean.True),
                'f' => ParseLiteral("false", JsonBoolean.False),
                'n' => ParseLiteral("null", JsonNull.Instance),
                '-' => ParseNumber(),
                var character when character is >= '0' and <= '9' => ParseNumber(),
                _ => throw Error("Expected a JSON value."),
            };
        }

        private JsonObject ParseObject()
        {
            _position++;
            SkipWhiteSpace();
            var result = new JsonObject();
            if (Consume('}'))
            {
                return result;
            }

            while (true)
            {
                if (_position == _text.Length || _text[_position] != '"')
                {
                    throw Error("Expected an object property name.");
                }
                var name = ParseString();
                SkipWhiteSpace();
                Expect(':');
                SkipWhiteSpace();
                if (result.Properties.ContainsKey(name))
                {
                    throw Error($"Duplicate object property '{name}'.");
                }
                result.Properties.Add(name, ParseValue());
                SkipWhiteSpace();
                if (Consume('}'))
                {
                    return result;
                }
                Expect(',');
                SkipWhiteSpace();
            }
        }

        private JsonArray ParseArray()
        {
            _position++;
            SkipWhiteSpace();
            var result = new JsonArray();
            if (Consume(']'))
            {
                return result;
            }

            while (true)
            {
                result.Items.Add(ParseValue());
                SkipWhiteSpace();
                if (Consume(']'))
                {
                    return result;
                }
                Expect(',');
                SkipWhiteSpace();
            }
        }

        private JsonValue ParseNumber()
        {
            var start = _position;
            Consume('-');
            if (Consume('0'))
            {
                if (_position < _text.Length && char.IsDigit(_text[_position]))
                {
                    throw Error("Leading zeroes are not permitted.");
                }
            }
            else
            {
                ReadDigits(required: true);
            }

            if (Consume('.'))
            {
                ReadDigits(required: true);
            }
            if (_position < _text.Length && _text[_position] is 'e' or 'E')
            {
                _position++;
                if (_position < _text.Length && _text[_position] is '+' or '-')
                {
                    _position++;
                }
                ReadDigits(required: true);
            }

            var value = _text.Substring(start, _position - start);
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ||
                double.IsInfinity(number))
            {
                throw Error("The JSON number is outside the supported range.");
            }
            return new JsonNumber(value);
        }

        private string ParseString()
        {
            Expect('"');
            var result = new StringBuilder();
            while (_position < _text.Length)
            {
                var character = _text[_position++];
                if (character == '"')
                {
                    return result.ToString();
                }
                if (character < ' ')
                {
                    throw Error("Control characters must be escaped.");
                }
                if (character != '\\')
                {
                    result.Append(character);
                    continue;
                }

                if (_position == _text.Length)
                {
                    throw Error("Incomplete escape sequence.");
                }
                var escaped = _text[_position++];
                result.Append(escaped switch
                {
                    '"' => '"',
                    '\\' => '\\',
                    '/' => '/',
                    'b' => '\b',
                    'f' => '\f',
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    'u' => ParseUnicodeEscape(),
                    _ => throw Error("Invalid escape sequence."),
                });
            }
            throw Error("Unterminated string.");
        }

        private char ParseUnicodeEscape()
        {
            if (_position + 4 > _text.Length ||
                !ushort.TryParse(
                    _text.Substring(_position, 4),
                    NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture,
                    out var value))
            {
                throw Error("Invalid Unicode escape sequence.");
            }
            _position += 4;
            return (char)value;
        }

        private T ParseLiteral<T>(string literal, T value)
            where T : JsonValue
        {
            if (_position + literal.Length > _text.Length ||
                string.CompareOrdinal(_text, _position, literal, 0, literal.Length) != 0)
            {
                throw Error($"Expected '{literal}'.");
            }
            _position += literal.Length;
            return value;
        }

        private void ReadDigits(bool required)
        {
            var start = _position;
            while (_position < _text.Length && char.IsDigit(_text[_position]))
            {
                _position++;
            }
            if (required && start == _position)
            {
                throw Error("Expected a digit.");
            }
        }

        private void SkipWhiteSpace()
        {
            while (_position < _text.Length && _text[_position] is ' ' or '\t' or '\r' or '\n')
            {
                _position++;
            }
        }

        private bool Consume(char character)
        {
            if (_position < _text.Length && _text[_position] == character)
            {
                _position++;
                return true;
            }
            return false;
        }

        private void Expect(char character)
        {
            if (!Consume(character))
            {
                throw Error($"Expected '{character}'.");
            }
        }

        private FormatException Error(string message)
            => new($"{message} Character position {_position}.");
    }
}

internal sealed class JsonObject : JsonValue
{
    public SortedDictionary<string, JsonValue> Properties { get; } =
        new(StringComparer.Ordinal);

    public override void WriteTo(StringBuilder builder)
    {
        builder.Append('{');
        var separator = false;
        foreach (var property in Properties)
        {
            if (separator)
            {
                builder.Append(',');
            }
            separator = true;
            WriteString(builder, property.Key);
            builder.Append(':');
            property.Value.WriteTo(builder);
        }
        builder.Append('}');
    }
}

internal sealed class JsonArray : JsonValue
{
    public List<JsonValue> Items { get; } = [];

    public override void WriteTo(StringBuilder builder)
    {
        builder.Append('[');
        for (var i = 0; i < Items.Count; i++)
        {
            if (i != 0)
            {
                builder.Append(',');
            }
            Items[i].WriteTo(builder);
        }
        builder.Append(']');
    }
}

internal sealed class JsonString(string value) : JsonValue
{
    public string Value { get; } = value;

    public override void WriteTo(StringBuilder builder)
        => WriteString(builder, Value);
}

internal sealed class JsonNumber(string value) : JsonValue
{
    public string Value { get; } = value;

    public override void WriteTo(StringBuilder builder)
        => builder.Append(Value);
}

internal sealed class JsonBoolean : JsonValue
{
    public static JsonBoolean True { get; } = new(true);
    public static JsonBoolean False { get; } = new(false);
    public bool Value { get; }

    private JsonBoolean(bool value)
    {
        Value = value;
    }

    public override void WriteTo(StringBuilder builder)
        => builder.Append(Value ? "true" : "false");
}

internal sealed class JsonNull : JsonValue
{
    public static JsonNull Instance { get; } = new();

    private JsonNull()
    {
    }

    public override void WriteTo(StringBuilder builder)
        => builder.Append("null");
}
