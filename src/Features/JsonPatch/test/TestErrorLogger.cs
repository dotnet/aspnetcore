// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.JsonPatch
{
    public class TestErrorLogger<T> where T : class
    {
        public string ErrorMessage { get; set; }

        public void LogErrorMessage(JsonPatchError patchError)
        {
            ErrorMessage = patchError.ErrorMessage;
        }
    }
}
