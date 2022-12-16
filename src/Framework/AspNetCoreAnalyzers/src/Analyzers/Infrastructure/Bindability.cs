// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure;
internal enum Bindability
{
    Bindable,
    NotBindable,
    InvalidReturnType
}
