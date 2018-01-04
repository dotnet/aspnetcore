// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class IdentityServiceResult
    {
        private static readonly IdentityServiceResult _success = new IdentityServiceResult { Succeeded = true };
        private List<IdentityServiceError> _errors = new List<IdentityServiceError>();

        public bool Succeeded { get; protected set; }

        public IEnumerable<IdentityServiceError> Errors => _errors;

        public static IdentityServiceResult Success => _success;

        public static IdentityServiceResult Failed(params IdentityServiceError[] errors)
        {
            var result = new IdentityServiceResult { Succeeded = false };
            if (errors != null)
            {
                result._errors.AddRange(errors);
            }
            return result;
        }
    }
}
