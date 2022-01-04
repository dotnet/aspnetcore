// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace FormatterWebSite;

public class SimpleTypePropertiesModel
{
    [Range(2, 8)]
    public byte ByteProperty { get; set; }

    [Range(2, 8)]
    public byte? NullableByteProperty { get; set; }

    [MinLength(2)]
    public byte[] ByteArrayProperty { get; set; }
}
