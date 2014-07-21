// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    /// <summary>
    /// Encapsulates information that describes an <see cref="OutputFormatter"/>.
    /// </summary>
    public class OutputFormatterDescriptor : OptionDescriptor<OutputFormatter>
    {
        /// <summary>
        /// Creates a new instance of <see cref="OutputFormatterDescriptor"/>.
        /// </summary>
        /// <param name="type">A <see cref="OutputFormatter/> type that the descriptor represents.
        /// </param>
        public OutputFormatterDescriptor([NotNull] Type type)
            : base(type)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="OutputFormatterDescriptor"/>.
        /// </summary>
        /// <param name="outputFormatter">An instance of <see cref="OutputFormatter"/>
        /// that the descriptor represents.</param>
        public OutputFormatterDescriptor([NotNull] OutputFormatter outputFormatter)
            : base(outputFormatter)
        {
        }
    }
}