// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    /// <summary>
    /// Logged to indicate the state of a class during controller discovery. Logs the type
    /// of the controller as well as the <see cref="ControllerStatus"/>.
    /// </summary>
    public class IsControllerValues : LoggerStructureBase
    {
        public IsControllerValues(Type type, ControllerStatus status)
        {
            Type = type;
            Status = status;
        }

        /// <summary>
        /// The <see cref="System.Type"/> of the potential <see cref="Controller"/> class.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// The <see cref="ControllerStatus"/> of the <see cref="Type"/>.
        /// </summary>
        public ControllerStatus Status { get; }

        public override string Format()
        {
            return LogFormatter.FormatStructure(this);
        }
    }
}