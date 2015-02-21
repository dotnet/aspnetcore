// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    /// <summary>
    /// Creates a set of <see cref="ControllerModel"/> for a type.
    /// </summary>
    public interface IControllerModelBuilder
    {
        /// <summary>
        /// Creates a set of <see cref="ControllerModel"/> for a type. May return null or empty if
        /// <paramref name="typeInfo"/> is not a controller type.
        /// </summary>
        /// <param name="typeInfo">The <see cref="TypeInfo"/>.</param>
        /// <returns>A <see cref="ControllerModel"/> or null.</returns>
        /// <remarks>
        /// Instances of <see cref="ControllerModel"/> returned from this interface should have their
        /// <see cref="ControllerModel.Actions"/> initialized.
        /// </remarks>
        ControllerModel BuildControllerModel([NotNull] TypeInfo typeInfo);
    }
}