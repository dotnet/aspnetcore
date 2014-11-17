// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Diagnostics.Entity.Utilities;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Diagnostics.Entity
{
    public class MigrationsEndPointOptions
    {
        public static PathString DefaultPath = new PathString("/ApplyDatabaseMigrations");
        private PathString _path = DefaultPath;

        public virtual PathString Path
        {
            get { return _path; }
            set
            {
                Check.NotNull(value, "value");
                _path = value;
            }
        }
    }
}
