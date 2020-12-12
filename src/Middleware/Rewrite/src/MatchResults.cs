// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Rewrite
{
    internal class MatchResults
    {
        public static readonly MatchResults EmptySuccess = new MatchResults(success: true);
        public static readonly MatchResults EmptyFailure = new MatchResults(success: false);

        public MatchResults(bool success, BackReferenceCollection? backReferences = null)
        {
            Success = success;
            BackReferences = backReferences;
        }

        public bool Success { get; }
        public BackReferenceCollection? BackReferences { get; }
    }
}
