using System.Globalization;

namespace BlazingPizza.Server.Model;

public class Topping
{
    public int Id { get; set; }

    public string Name { get; set; }

    public decimal Price { get; set; }

    public string GetFormattedPrice() => Price.ToString("0.00", CultureInfo.CurrentCulture);
}
