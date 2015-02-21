// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    /// <summary>
    /// Creates a set of <see cref="ActionModel"/> for a method.
    /// </summary>
    public interface IActionModelBuilder
    {
        /// <summary>
        /// Creates a set of <see cref="ActionModel"/> for a method. May return null or empty if
        /// <paramref name="methodInfo"/> is not an action method.
        /// </summary>
        /// <param name="methodInfo">The <see cref="MethodInfo"/>.</param>
        /// <param name="typeInfo">The <see cref="TypeInfo"/>.</param>
        /// <returns>A set of <see cref="ActionModel"/> or null.</returns>
        /// <remarks>
        /// Instances of <see cref="ActionModel"/> returned from this interface should have their
        /// <see cref="ActionModel.Parameters"/> initialized.
        /// </remarks>
        IEnumerable<ActionModel> BuildActionModels([NotNull] TypeInfo typeInfo, [NotNull] MethodInfo methodInfo);
    }
}