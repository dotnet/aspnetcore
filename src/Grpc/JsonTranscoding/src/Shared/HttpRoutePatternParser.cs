// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Grpc.Shared;

// HTTP Template Grammar:
//
// Template = "/" | "/" Segments [ Verb ] ;
// Segments = Segment { "/" Segment } ;
// Segment  = "*" | "**" | LITERAL | Variable ;
// Variable = "{" FieldPath [ "=" Segments ] "}" ;
// FieldPath = IDENT { "." IDENT } ;
// Verb     = ":" LITERAL ;
internal class HttpRoutePatternParser
{
    private readonly string _input;

    // Token delimiter indexes
    private int _tokenStart;
    private int _tokenEnd;

    private bool _inVariable;

    private readonly List<string> _segments;
    private string? _verb;
    private readonly List<HttpRouteVariable> _variables;
    private bool _hasCatchAllSegment;

    public List<string> Segments => _segments;
    public string? Verb => _verb;
    public List<HttpRouteVariable> Variables => _variables;

    public HttpRoutePatternParser(string input)
    {
        _input = input;
        _segments = new List<string>();
        _variables = new List<HttpRouteVariable>();
    }

    public void Parse()
    {
        try
        {
            ParseTemplate();

            if (_tokenStart < _input.Length)
            {
                throw new InvalidOperationException("Path template wasn't parsed to the end.");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error parsing path template '{_input}'.", ex);
        }
    }

    // Template = "/" Segments [ Verb ] ;
    private void ParseTemplate()
    {
        if (!Consume('/'))
        {
            throw new InvalidOperationException("Path template must start with a '/'.");
        }
        ParseSegments();

        if (EnsureCurrent())
        {
            if (CurrentChar != ':')
            {
                throw new InvalidOperationException("Path segment must end with a '/'.");
            }
            ParseVerb();
        }
    }

    // Segments = Segment { "/" Segment } ;
    private void ParseSegments()
    {
        while (true)
        {
            if (!ParseSegment())
            {
                // Support '/' template.
                if (_segments.Count > 0)
                {
                    throw new InvalidOperationException("Route template shouldn't end with a '/'.");
                }
            }
            if (!Consume('/'))
            {
                break;
            }
        }
    }

    // Segment  = "*" | "**" | LITERAL | Variable ;
    private bool ParseSegment()
    {
        if (!EnsureCurrent())
        {
            return false;
        }
        switch (CurrentChar)
        {
            case '*':
                {
                    if (_hasCatchAllSegment)
                    {
                        throw new InvalidOperationException("Only literal segments can follow a catch-all segment.");
                    }

                    ConsumeAndAssert('*');

                    // Check for '**'
                    if (Consume('*'))
                    {
                        _segments.Add("**");
                        _hasCatchAllSegment = true;
                        if (_inVariable)
                        {
                            CurrentVariable.HasCatchAllPath = true;
                        }
                        return true;
                    }
                    else
                    {
                        _segments.Add("*");
                        return true;
                    }
                }

            case '{':
                if (_hasCatchAllSegment)
                {
                    throw new InvalidOperationException("Only literal segments can follow a catch-all segment.");
                }

                ParseVariable();
                return true;
            default:
                ParseLiteralSegment();
                return true;
        }
    }

    // Variable = "{" FieldPath [ "=" Segments ] "}" ;
    private void ParseVariable()
    {
        ConsumeAndAssert('{');
        StartVariable();
        ParseFieldPath();
        if (Consume('='))
        {
            ParseSegments();
        }
        else
        {
            _segments.Add("*");
        }
        EndVariable();
        ConsumeAndAssert('}');
    }

    private void ParseLiteralSegment()
    {
        if (!TryParseLiteral(out var literal))
        {
            throw new InvalidOperationException("Empty literal segment.");
        }
        _segments.Add(literal);
    }

    // FieldPath = IDENT { "." IDENT } ;
    private void ParseFieldPath()
    {
        do
        {
            if (!ParseIdentifier())
            {
                throw new InvalidOperationException("Incomplete or empty field path.");
            }
        }
        while (Consume('.'));
    }

    // Verb     = ":" LITERAL ;
    private void ParseVerb()
    {
        ConsumeAndAssert(':');
        if (!TryParseLiteral(out _verb))
        {
            throw new InvalidOperationException("Empty verb.");
        }
    }

    private bool ParseIdentifier()
    {
        var identifier = string.Empty;
        var hasEndChar = false;

        while (!hasEndChar && NextChar())
        {
            var c = CurrentChar;
            switch (c)
            {
                case '.':
                case '}':
                case '=':
                    hasEndChar = true;
                    break;
                default:
                    Consume(c);
                    identifier += c;
                    break;
            }
        }

        if (string.IsNullOrEmpty(identifier))
        {
            return false;
        }

        CurrentVariable.FieldPath.Add(identifier);
        return true;
    }

    private bool TryParseLiteral([NotNullWhen(true)] out string? literal)
    {
        literal = null;

        if (!EnsureCurrent())
        {
            return false;
        }

        // Initialize to false in case we encounter an empty literal.
        var result = false;

        while (true)
        {
            var c = CurrentChar;
            switch (c)
            {
                case '/':
                case ':':
                case '}':
                    if (!result)
                    {
                        throw new InvalidOperationException("Path template has an empty segment.");
                    }
                    return result;
                default:
                    Consume(c);
                    literal += c;
                    break;
            }

            result = true;

            if (!NextChar())
            {
                break;
            }
        }

        return result;
    }

    private void ConsumeAndAssert(char? c)
    {
        if (!Consume(c))
        {
            throw new InvalidOperationException($"Expected '{c}' when parsing path template.");
        }
    }

    private bool Consume(char? c)
    {
        if (!EnsureCurrent())
        {
            return false;
        }
        if (CurrentChar != c)
        {
            return false;
        }
        _tokenStart++;
        return true;
    }

    private bool EnsureCurrent() => _tokenStart < _tokenEnd || NextChar();

    private bool NextChar()
    {
        if (_tokenEnd < _input.Length)
        {
            _tokenEnd++;
            return true;
        }
        else
        {
            return false;
        }
    }

    private char? CurrentChar => _tokenStart < _tokenEnd && _tokenEnd <= _input.Length ? _input[_tokenEnd - 1] : null;

    private HttpRouteVariable CurrentVariable
    {
        get
        {
            if (!_inVariable || _variables.LastOrDefault() is not HttpRouteVariable variable)
            {
                throw new InvalidOperationException("Unexpected error when updating variable.");
            }

            return variable;
        }

    }

    private void StartVariable()
    {
        if (_inVariable)
        {
            throw new InvalidOperationException("Variable can't be nested.");
        }

        _variables.Add(new HttpRouteVariable());
        _inVariable = true;
        CurrentVariable.StartSegment = _segments.Count;
        CurrentVariable.HasCatchAllPath = false;
    }

    private void EndVariable()
    {
        CurrentVariable.EndSegment = _segments.Count;

        Debug.Assert(CurrentVariable.FieldPath.Any());
        Debug.Assert(CurrentVariable.StartSegment < CurrentVariable.EndSegment);
        Debug.Assert(CurrentVariable.EndSegment <= _segments.Count);

        _inVariable = false;
    }
}
