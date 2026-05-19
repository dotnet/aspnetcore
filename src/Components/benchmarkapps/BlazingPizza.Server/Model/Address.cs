using System.ComponentModel.DataAnnotations;

namespace BlazingPizza.Server.Model;

public class Address
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; }

    [Required, MaxLength(100)]
    public string Line1 { get; set; }

    [MaxLength(100)]
    public string Line2 { get; set; }

    [Required, MaxLength(50)]
    public string City { get; set; }

    [Required, MaxLength(20)]
    public string Region { get; set; }

    [Required, MaxLength(20)]
    public string PostalCode { get; set; }
}
