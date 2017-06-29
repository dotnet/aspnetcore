// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    public class RazorDiagnosticSerializer
    {
        public static string Serialize(RazorDiagnostic diagnostic)
        {
            return diagnostic.ToString();
        }
    }
}
