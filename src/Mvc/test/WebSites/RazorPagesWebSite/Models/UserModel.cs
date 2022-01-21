// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace RazorPagesWebSite;

public class UserModel : IUserModel
{
    [Required]
    public string Name { get; set; }

    [Range(0, 99)]
    public int Age { get; set; }

    public override string ToString()
    {
        return $"Name = {Name}, Age = {Age}";
    }
}
