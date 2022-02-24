// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace IdentitySample.Models.ManageViewModels;

public class DisplayRecoveryCodesViewModel
{
    [Required]
    public IEnumerable<string> Codes { get; set; }

}
