namespace BlazingPizza.Server.Model;

public class ToppingsService
{
    public IList<Topping> GetToppings()
    {
        return new Topping[]
        {
            new Topping()
            {
                Name = "Extra cheese",
                Price = 2.50m,
            },
            new Topping()
            {
                Name = "American bacon",
                Price = 2.99m,
            },
            new Topping()
            {
                Name = "British bacon",
                Price = 2.99m,
            },
            new Topping()
            {
                Name = "Canadian bacon",
                Price = 2.99m,
            },
            new Topping()
            {
                Name = "Tea and crumpets",
                Price = 5.00m
            },
            new Topping()
            {
                Name = "Fresh-baked scones",
                Price = 4.50m,
            },
            new Topping()
            {
                Name = "Bell peppers",
                Price = 1.00m,
            },
            new Topping()
            {
                Name = "Onions",
                Price = 1.00m,
            },
            new Topping()
            {
                Name = "Mushrooms",
                Price = 1.00m,
            },
            new Topping()
            {
                Name = "Pepperoni",
                Price = 1.00m,
            },
            new Topping()
            {
                Name = "Duck sausage",
                Price = 3.20m,
            },
            new Topping()
            {
                Name = "Venison meatballs",
                Price = 2.50m,
            },
            new Topping()
            {
                Name = "Served on a silver platter",
                Price = 250.99m,
            },
            new Topping()
            {
                Name = "Lobster on top",
                Price = 64.50m,
            },
            new Topping()
            {
                Name = "Sturgeon caviar",
                Price = 101.75m,
            },
            new Topping()
            {
                Name = "Artichoke hearts",
                Price = 3.40m,
            },
            new Topping()
            {
                Name = "Fresh tomatoes",
                Price = 1.50m,
            },
            new Topping()
            {
                Name = "Basil",
                Price = 1.50m,
            },
            new Topping()
            {
                Name = "Steak (medium-rare)",
                Price = 8.50m,
            },
            new Topping()
            {
                Name = "Blazing hot peppers",
                Price = 4.20m,
            },
            new Topping()
            {
                Name = "Buffalo chicken",
                Price = 5.00m,
            },
            new Topping()
            {
                Name = "Blue cheese",
                Price = 2.50m,
            },
        };
    }
}
