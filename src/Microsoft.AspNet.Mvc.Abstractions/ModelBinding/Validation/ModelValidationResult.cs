// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class ModelValidationResult
    {
        public ModelValidationResult(string memberName, string message)
        {
            MemberName = memberName ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public string MemberName { get; private set; }

        public string Message { get; private set; }
    }
}
