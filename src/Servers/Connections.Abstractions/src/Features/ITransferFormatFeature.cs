// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Connections.Features
{
    /// <summary>
    /// Gets the transfer format of the protocol.
    /// </summary>
    public interface ITransferFormatFeature
    {
        /// <summary>
        /// Gets the supported <see cref="TransferFormat"/>.
        /// </summary>
        TransferFormat SupportedFormats { get; }

        /// <summary>
        /// Gets or sets the active <see cref="TransferFormat"/>.
        /// </summary>
        TransferFormat ActiveFormat { get; set; }
    }
}
