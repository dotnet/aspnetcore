// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Delegate that determines if the specified type is excluded from validation.
    /// </summary>
    /// <param name="type"><see cref="Type"/> which needs to be checked.</param>
    /// <returns><see cref="true"/> if excluded, <see cref="false"/> otherwise.</returns>
    public delegate bool ExcludeFromValidationDelegate(Type type);
}