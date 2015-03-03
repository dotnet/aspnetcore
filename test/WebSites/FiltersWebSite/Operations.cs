// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Authorization;

namespace FiltersWebSite
{
    public static class Operations
    {
        public static OperationAuthorizationRequirement Edit = new OperationAuthorizationRequirement { Name = "Edit" };
        public static OperationAuthorizationRequirement Create = new OperationAuthorizationRequirement { Name = "Create" };
        public static OperationAuthorizationRequirement Delete = new OperationAuthorizationRequirement { Name = "Delete" };
    }
}
