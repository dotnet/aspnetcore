// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Diagnostics.Entity
{
    public class MigrationsEndPointOptions
    {
        public static PathString DefaultPath = new PathString("/ApplyDatabaseMigrations");

        public virtual PathString Path { get; set; } = DefaultPath;
    }
}
