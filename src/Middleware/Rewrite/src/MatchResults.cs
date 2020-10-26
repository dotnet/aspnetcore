// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Rewrite
{
    internal class MatchResults
    {
        public static readonly MatchResults EmptySuccess = new MatchResults { Success = true };
        public static readonly MatchResults EmptyFailure = new MatchResults { Success = false };

        public bool Success { get; set; }
        public BackReferenceCollection BackReferences { get; set; }
    }
}
