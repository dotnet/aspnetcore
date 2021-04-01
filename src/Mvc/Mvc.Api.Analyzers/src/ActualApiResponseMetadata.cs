// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    internal readonly struct ActualApiResponseMetadata
    {
        private readonly int? _statusCode;

        public ActualApiResponseMetadata(ReturnStatementSyntax returnStatement, ITypeSymbol returnType)
        {
            ReturnStatement = returnStatement;
            ReturnType = returnType;
            _statusCode = null;
        }

        public ActualApiResponseMetadata(ReturnStatementSyntax returnStatement, int statusCode, ITypeSymbol? returnType)
        {
            ReturnStatement = returnStatement;
            _statusCode = statusCode;
            ReturnType = returnType;
        }

        public ReturnStatementSyntax ReturnStatement { get; }

        public int StatusCode => _statusCode ?? throw new ArgumentException("Status code is not available when IsDefaultResponse is true");

        public bool IsDefaultResponse => _statusCode == null;

        public ITypeSymbol? ReturnType { get; }
    }
}
