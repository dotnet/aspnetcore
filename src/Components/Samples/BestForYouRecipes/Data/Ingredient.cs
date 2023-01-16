// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace BestForYouRecipes.Data;

public class Ingredient
{
    public double? Quantity { get; set; } // Optional because ingredients may be listed without a quantity (e.g., "seasoning")
    public IngredientUnit? Unit { get; set; }
    public required string Name { get; set; }

    public override string ToString()
    {
        if (Quantity.HasValue)
        {
            return Unit.HasValue ? $"{Quantity} {Unit?.ToString().ToLowerInvariant()} {Name}" : $"{Quantity} {Name}";
        }
        else
        {
            return Name;
        }
    }

    public static Ingredient Parse(string text, bool metric)
    {
        var result = new Ingredient { Name = "" };

        // If it starts with a number, treat that as the quantity
        text = text.Trim();
        var leadingNumberMatch = Regex.Match(text, "^\\d+([\\.\\,]\\d+)?");
        if (leadingNumberMatch.Success && double.TryParse(leadingNumberMatch.Value, out var quantity))
        {
            result.Quantity = quantity;
            text = text.Substring(leadingNumberMatch.Value.Length).Trim();

            // Now see if we can extract a known unit
            var nextWord = Regex.Match(text, "^[A-Za-z]+\\b");
            if (nextWord.Success)
            {
                result.Unit = UnitFromText(nextWord.Value.ToLowerInvariant());
                if (result.Unit.HasValue)
                {
                    text = text.Substring(nextWord.Value.Length).Trim();
                }
            }
        }

        // Whatever remains is the text
        result.Name = text;
        result.SetSystem(metric);
        return result;
    }

    private static IngredientUnit? UnitFromText(ReadOnlySpan<char> text)
    {
        if (UnitFromTextCore(text) is { } result)
        {
            return result;
        }

        // Try without trailing 's'
        return text.EndsWith("s") ? UnitFromTextCore(text[0..^1]) : default;
    }

    private static IngredientUnit? UnitFromTextCore(ReadOnlySpan<char> text)
    {
        return text switch
        {
            "g" => IngredientUnit.Grams,
            "gr" => IngredientUnit.Grams,
            "gram" => IngredientUnit.Grams,

            "kg" => IngredientUnit.Kilograms,
            "kilo" => IngredientUnit.Kilograms,
            "kilogram" => IngredientUnit.Kilograms,

            "ml" => IngredientUnit.Millileters,
            "millileter" => IngredientUnit.Millileters,

            "l" => IngredientUnit.Liters,
            "liter" => IngredientUnit.Liters,
            "litre" => IngredientUnit.Liters,

            "tsp" => IngredientUnit.Teaspoons,
            "teaspoon" => IngredientUnit.Teaspoons,

            "tbsp" => IngredientUnit.Tablespoons,
            "tblsp" => IngredientUnit.Tablespoons,
            "tablespoon" => IngredientUnit.Tablespoons,

            "oz" => IngredientUnit.Ounces,
            "ounce" => IngredientUnit.Ounces,

            "lb" => IngredientUnit.Pounds,
            "pound" => IngredientUnit.Pounds,

            "floz" => IngredientUnit.FluidOunces,

            "gal" => IngredientUnit.Gallons,
            "gallon" => IngredientUnit.Gallons,

            "c" => IngredientUnit.Cups,
            "cp" => IngredientUnit.Cups,
            "cup" => IngredientUnit.Cups,

            "bushel" => IngredientUnit.Bushels,
            "af" => IngredientUnit.AcreFeet,
            "acrefeet" => IngredientUnit.AcreFeet,
            "acrefoot" => IngredientUnit.AcreFeet,

            _ => null,
        };
    }

    public void SetSystem(bool metric)
    {
        if (SetSystemExact(metric))
        {
            Quantity = Math.Round(Quantity!.Value * 10) / 10.0;
        }
    }

    private bool SetSystemExact(bool metric)
    {
        if (!Unit.HasValue || IsMetricUnit(Unit.Value) == metric)
        {
            // No change required
            return false;
        }

        if (TryGetWeightInGrams(out var grams))
        {
            if (metric)
            {
                if (grams < 50)
                {
                    Quantity = grams;
                    Unit = IngredientUnit.Grams;
                }
                else if (grams < Kg_to_g)
                {
                    Quantity = 10 * Math.Round(grams / 10.0);
                    Unit = IngredientUnit.Grams;
                }
                else
                {
                    Quantity = grams / Kg_to_g;
                    Unit = IngredientUnit.Kilograms;
                }
            }
            else
            {
                if (grams < Lb_to_g)
                {
                    Quantity = grams / Oz_to_g;
                    Unit = IngredientUnit.Ounces;
                }
                else
                {
                    Quantity = grams / Lb_to_g;
                    Unit = IngredientUnit.Pounds;
                }
            }
            return true;
        }
        else if (TryGetVolumeInMillileters(out var ml))
        {
            if (metric)
            {
                if (ml < 50)
                {
                    Quantity = ml;
                    Unit = IngredientUnit.Millileters;
                }
                else if (ml < L_to_ml)
                {
                    Quantity = 10 * Math.Round(ml / 10.0);
                    Unit = IngredientUnit.Millileters;
                }
                else
                {
                    Quantity = ml / L_to_ml;
                    Unit = IngredientUnit.Liters;
                }
            }
            else
            {
                if (ml < Tbsp_to_ml)
                {
                    Quantity = ml / Tsp_to_ml;
                    Unit = IngredientUnit.Teaspoons;
                }
                else if (ml < Floz_to_ml)
                {
                    Quantity = ml / Tbsp_to_ml;
                    Unit = IngredientUnit.Tablespoons;
                }
                else if (ml < Cup_to_ml)
                {
                    Quantity = ml / Floz_to_ml;
                    Unit = IngredientUnit.FluidOunces;
                }
                else if (ml < Gal_to_ml)
                {
                    Quantity = ml / Cup_to_ml;
                    Unit = IngredientUnit.Cups;
                }
                else if (ml < Bushel_to_ml)
                {
                    Quantity = ml / Gal_to_ml;
                    Unit = IngredientUnit.Gallons;
                }
                else if (ml < AcreFoot_to_ml)
                {
                    Quantity = ml / Bushel_to_ml;
                    Unit = IngredientUnit.Bushels;
                }
                else
                {
                    Quantity = ml / AcreFoot_to_ml;
                    Unit = IngredientUnit.AcreFeet;
                }
            }
            return true;
        }

        return false;
    }

    const double L_to_ml = 1000;
    const double Tsp_to_ml = 4.92892;
    const double Tbsp_to_ml = 14.7868;
    const double Floz_to_ml = 29.5735;
    const double Gal_to_ml = 3785.41;
    const double Cup_to_ml = 236.588;
    const double Bushel_to_ml = 35239.1;
    const double AcreFoot_to_ml = 1.233e+9;
    const double Kg_to_g = 1000;
    const double Oz_to_g = 28.3495;
    const double Lb_to_g = 453.592;

    private bool TryGetWeightInGrams(out double grams)
    {
        if (Unit.HasValue && Quantity.HasValue)
        {
            switch (Unit)
            {
                case IngredientUnit.Grams:
                    grams = Quantity.Value;
                    return true;
                case IngredientUnit.Kilograms:
                    grams = Quantity.Value * Kg_to_g;
                    return true;
                case IngredientUnit.Ounces:
                    grams = Quantity.Value * Oz_to_g;
                    return true;
                case IngredientUnit.Pounds:
                    grams = Quantity.Value * Lb_to_g;
                    return true;
            }
        }

        grams = default;
        return false;
    }

    private bool TryGetVolumeInMillileters(out double ml)
    {
        if (Unit.HasValue && Quantity.HasValue)
        {
            switch (Unit)
            {
                case IngredientUnit.Millileters:
                    ml = Quantity.Value;
                    return true;
                case IngredientUnit.Liters:
                    ml = Quantity.Value * L_to_ml;
                    return true;

                case IngredientUnit.Teaspoons:
                    ml = Quantity.Value * Tsp_to_ml;
                    return true;

                case IngredientUnit.Tablespoons:
                    ml = Quantity.Value * Tbsp_to_ml;
                    return true;

                case IngredientUnit.FluidOunces:
                    ml = Quantity.Value * Floz_to_ml;
                    return true;

                case IngredientUnit.Gallons:
                    ml = Quantity.Value * Gal_to_ml;
                    return true;

                case IngredientUnit.Cups:
                    ml = Quantity.Value * Cup_to_ml;
                    return true;

                case IngredientUnit.Bushels:
                    ml = Quantity.Value * Bushel_to_ml;
                    return true;

                case IngredientUnit.AcreFeet:
                    ml = Quantity.Value * AcreFoot_to_ml;
                    return true;
            }
        }

        ml = default;
        return false;
    }

    private static bool IsMetricUnit(IngredientUnit unit)
    {
        switch (unit)
        {
            case IngredientUnit.Grams:
            case IngredientUnit.Kilograms:
            case IngredientUnit.Liters:
            case IngredientUnit.Millileters:
                return true;
            default:
                return false;
        }
    }
}

public enum IngredientUnit
{
    Grams,
    Kilograms,
    Ounces,
    Pounds,
    Liters,
    Millileters,
    Teaspoons,
    Tablespoons,
    FluidOunces,
    Gallons,
    Cups,
    Bushels,
    AcreFeet,
}
