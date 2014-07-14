// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Encapsulates information that describes an <see cref="OutputFormatter"/>.
    /// </summary>
    public class OutputFormatterDescriptor
    {
        /// <summary>
        /// Creates a new instance of <see cref="OutputFormatterDescriptor"/>.
        /// </summary>
        /// <param name="outputFormatterType">A <see cref="OutputFormatter/> type that the descriptor represents.
        /// </param>
        public OutputFormatterDescriptor([NotNull] Type outputFormatterType)
        {
            var formatterType = typeof(OutputFormatter);
            if (!formatterType.IsAssignableFrom(outputFormatterType))
            {
                var message = Resources.FormatTypeMustDeriveFromType(outputFormatterType,
                                                                     formatterType.FullName);
                throw new ArgumentException(message, "outputFormatterType");
            }

            OutputFormatterType = outputFormatterType;
        }

        /// <summary>
        /// Creates a new instance of <see cref="OutputFormatterDescriptor"/>.
        /// </summary>
        /// <param name="outputFormatter">An instance of <see cref="OutputFormatter"/>
        ///  that the descriptor represents.</param>
        public OutputFormatterDescriptor([NotNull] OutputFormatter outputFormatter)
        {
            OutputFormatter = outputFormatter;
            OutputFormatterType = outputFormatter.GetType();
        }

        /// <summary>
        /// Gets the type of the <see cref="OutputFormatter"/>.
        /// </summary>
        public Type OutputFormatterType
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the instance of the <see cref="OutputFormatter"/>.
        /// </summary>
        public OutputFormatter OutputFormatter
        {
            get;
            private set;
        }
    }
}