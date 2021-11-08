// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Extensions.Localization;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
internal interface IResourceStringProvider
{
    IList<string>? GetAllResourceStrings(CultureInfo culture, bool throwOnMissing);
}
