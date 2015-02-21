// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    /// <summary>
    /// Logging representation of a <see cref="ParameterModel"/>. Logged as a substructure of
    /// <see cref="ActionModelValues"/>, this contains the name, type, and
    /// binder metadata of the parameter.
    /// </summary>
    public class ParameterModelValues : LoggerStructureBase
    {
        public ParameterModelValues([NotNull] ParameterModel inner)
        {
            ParameterName = inner.ParameterName;
            ParameterType = inner.ParameterInfo.ParameterType;
            BinderMetadata = inner.BinderMetadata?.GetType();
        }

        /// <summary>
        /// The name of the parameter. See <see cref="ParameterModel.ParameterName"/>.
        /// </summary>
        public string ParameterName { get; }

        /// <summary>
        /// The <see cref="Type"/> of the parameter.
        /// </summary>
        public Type ParameterType { get; }

        /// <summary>
        /// The <see cref="Type"/> of the <see cref="ParameterModel.BinderMetadata"/>.
        /// </summary>
        public Type BinderMetadata { get; }

        public override string Format()
        {
            return LogFormatter.FormatStructure(this);
        }
    }
}