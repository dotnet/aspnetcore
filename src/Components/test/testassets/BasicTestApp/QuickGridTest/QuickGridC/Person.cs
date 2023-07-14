// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BasicTestApp.QuickGridTest.QuickGridC;

public class Person
{
    [Key] public int Id { get; set; }

    [StringLength(50)] public string FirstName { get; set; } = default!;

    [StringLength(50)] public string LastName { get; set; }
    public DateTimeOffset? BirthDate { get; set; }
    [Column(TypeName = "nvarchar")][StringLength(10)] public Gender? Gender { get; set; }
    [Column(TypeName = "decimal(18, 2)")]
    public decimal? Sold { get; set; }
    public bool? Active { get; set; }
}
