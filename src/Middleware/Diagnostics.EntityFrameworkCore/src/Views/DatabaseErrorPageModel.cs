// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Views
{
    internal class DatabaseErrorPageModel
    {
        public DatabaseErrorPageModel(
            Exception exception,
            IEnumerable<DatabaseContextDetails> contextDetails,
            DatabaseErrorPageOptions options,
            PathString pathBase)
        {
            Exception = exception;
            ContextDetails = contextDetails;
            Options = options;
            PathBase = pathBase;
        }

        public virtual Exception Exception { get; }
        public virtual IEnumerable<DatabaseContextDetails> ContextDetails { get; }
        public virtual DatabaseErrorPageOptions Options { get; }
        public virtual PathString PathBase { get; }
    }
}
