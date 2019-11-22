// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Extensions.ApiDescription.Tasks
{
    /// <summary>
    /// Restore <see cref="ITaskItem"/>s from given property value.
    /// </summary>
    public class GetCurrentItems : Task
    {
        /// <summary>
        /// The property value to deserialize.
        /// </summary>
        [Required]
        public string Input { get; set; }

        /// <summary>
        /// The restored <see cref="ITaskItem"/>s. Will never contain more than one item.
        /// </summary>
        [Output]
        public ITaskItem[] Outputs { get; set; }

        /// <inheritdoc />
        public override bool Execute()
        {
            Outputs = new[] { MetadataSerializer.DeserializeMetadata(Input) };

            return true;
        }
    }
}
