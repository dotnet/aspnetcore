// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Rewrite.Internal
{
    public class MatchResults
    {
        public static readonly MatchResults EmptySuccess = new MatchResults { BackReference = null, Success = true };
        public static readonly MatchResults EmptyFailure = new MatchResults { BackReference = null, Success = false };

        public GroupCollection BackReference { get; set; }
        public bool Success { get; set; }
    }
}
