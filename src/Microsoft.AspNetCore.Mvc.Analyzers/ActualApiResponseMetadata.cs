// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    internal readonly struct ActualApiResponseMetadata
    {
        private readonly int? _statusCode;

        public ActualApiResponseMetadata(ReturnStatementSyntax returnStatement)
        {
            ReturnStatement = returnStatement;
            _statusCode = null;
        }

        public ActualApiResponseMetadata(ReturnStatementSyntax returnStatement, int statusCode)
        {
            ReturnStatement = returnStatement;
            _statusCode = statusCode;
        }

        public ReturnStatementSyntax ReturnStatement { get; }

        public int StatusCode => _statusCode.Value;

        public bool IsDefaultResponse => _statusCode == null;
    }
}
