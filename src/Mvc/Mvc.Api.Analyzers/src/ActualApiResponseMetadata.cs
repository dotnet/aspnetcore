// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    internal readonly struct ActualApiResponseMetadata
    {
        private readonly int? _statusCode;

        public ActualApiResponseMetadata(ExpressionSyntax returnExpression, ITypeSymbol returnType)
        {
            ReturnExpression = returnExpression;
            ReturnType = returnType;
            _statusCode = null;
        }

        public ActualApiResponseMetadata(ExpressionSyntax returnExpression, int statusCode, ITypeSymbol? returnType)
        {
            ReturnExpression = returnExpression;
            _statusCode = statusCode;
            ReturnType = returnType;
        }

        public ExpressionSyntax ReturnExpression { get; }

        public int StatusCode => _statusCode ?? throw new ArgumentException("Status code is not available when IsDefaultResponse is true");

        public bool IsDefaultResponse => _statusCode == null;

        public ITypeSymbol? ReturnType { get; }
    }
}
