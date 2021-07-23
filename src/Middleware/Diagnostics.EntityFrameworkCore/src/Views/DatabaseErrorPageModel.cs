// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
