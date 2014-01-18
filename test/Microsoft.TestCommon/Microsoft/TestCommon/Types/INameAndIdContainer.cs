// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace Microsoft.TestCommon.Types
{
    /// <summary>
    /// Tagging interface to assist comparing instances of these types.
    /// </summary>
    public interface INameAndIdContainer
    {
        string Name { get; set; }

        int Id { get; set; }
    }
}
