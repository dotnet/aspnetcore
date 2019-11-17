// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.JsonPatch
{
    public partial class JsonPatchDocument<TModel> : Microsoft.AspNetCore.JsonPatch.IJsonPatchDocument where TModel : class
    {
        internal string GetPath<TProp>(System.Linq.Expressions.Expression<System.Func<TModel, TProp>> expr, string position) { throw null; }
    }
}
