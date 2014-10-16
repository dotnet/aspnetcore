// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Delegate that determines if the specified <paramref name="type"/> is excluded from validation.
    /// </summary>
    /// <param name="type"><see cref="Type"/> which needs to be checked.</param>
    /// <returns><c>true</c> if excluded, <c>false</c> otherwise.</returns>
    public delegate bool ExcludeFromValidationDelegate(Type type);
}