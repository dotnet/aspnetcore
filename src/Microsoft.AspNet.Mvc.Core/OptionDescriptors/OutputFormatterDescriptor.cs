// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    /// <summary>
    /// Encapsulates information that describes an <see cref="IOutputFormatter"/>.
    /// </summary>
    public class OutputFormatterDescriptor : OptionDescriptor<IOutputFormatter>
    {
        /// <summary>
        /// Creates a new instance of <see cref="OutputFormatterDescriptor"/>.
        /// </summary>
        /// <param name="type">A <see cref="IOutputFormatter"/> type that the descriptor represents.
        /// </param>
        public OutputFormatterDescriptor([NotNull] Type type)
            : base(type)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="OutputFormatterDescriptor"/>.
        /// </summary>
        /// <param name="outputFormatter">An instance of <see cref="IOutputFormatter"/>
        /// that the descriptor represents.</param>
        public OutputFormatterDescriptor([NotNull] IOutputFormatter outputFormatter)
            : base(outputFormatter)
        {
        }
    }
}