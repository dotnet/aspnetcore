// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace TagHelpersWebSite.Models;

public class Employee
{
    public int EmployeeId { get; set; }

    [Display(Name = "Full Name", ShortName = "FN")]
    public string FullName { get; set; }

    [DisplayFormat(NullDisplayText = "Not specified")]
    public string Gender { get; set; }

    [Range(10, 100)]
    public int Age { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTimeOffset? JoinDate { get; set; }

    [DataType(DataType.EmailAddress)]
    public string Email { get; set; }

    [DisplayFormat(NullDisplayText = "Not specified")]
    public int? Salary { get; set; }
}
