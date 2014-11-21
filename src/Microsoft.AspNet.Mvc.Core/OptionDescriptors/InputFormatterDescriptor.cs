// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    /// <summary>
    /// Encapsulates information that describes an <see cref="IInputFormatter"/>.
    /// </summary>
    public class InputFormatterDescriptor : OptionDescriptor<IInputFormatter>
    {
        /// <summary>
        /// Creates a new instance of <see cref="InputFormatterDescriptor"/>.
        /// </summary>
        /// <param name="type">A <see cref="IOutputFormatter"/> type that the descriptor represents.
        /// </param>
        public InputFormatterDescriptor([NotNull] Type type)
            : base(type)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="InputFormatterDescriptor"/>.
        /// </summary>
        /// <param name="inputFormatter">An instance of <see cref="IInputFormatter"/>
        /// that the descriptor represents.</param>
        public InputFormatterDescriptor([NotNull] IInputFormatter inputFormatter)
            : base(inputFormatter)
        {
        }
    }
}