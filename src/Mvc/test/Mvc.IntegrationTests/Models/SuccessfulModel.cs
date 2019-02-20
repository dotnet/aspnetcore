// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    public class SuccessfulModel
    {
        [ModelBinder(typeof(SuccessfulModelBinder))]
        public bool IsBound { get; set; }

        public string Name { get; set; }
    }
}
