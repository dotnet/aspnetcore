// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

[JsonDerivedType(typeof(Triangle), typeDiscriminator: "triangle")]
[JsonDerivedType(typeof(Square), typeDiscriminator: "square")]
internal abstract class Shape
{
    public string Color { get; set; } = string.Empty;
    public int Sides { get; set; }
}

internal class Triangle : Shape
{
    public double Hypotenuse { get; set; }
}
internal class Square : Shape
{
    public double Area { get; set; }
}

[JsonDerivedType(typeof(WeatherForecastWithCity), 0)]
[JsonDerivedType(typeof(WeatherForecastWithTimeSeries), 1)]
[JsonDerivedType(typeof(WeatherForecastWithLocalNews), 2)]
internal abstract class WeatherForecastBase { }

internal class WeatherForecastWithCity : WeatherForecastBase
{
    public required string City { get; set; }
}

internal class WeatherForecastWithTimeSeries : WeatherForecastBase
{
    public DateTimeOffset Date { get; set; }
    public int TemperatureC { get; set; }
    public required string Summary { get; set; }
}

internal class WeatherForecastWithLocalNews : WeatherForecastBase
{
    public required string News { get; set; }
}

[JsonDerivedType(typeof(Student), typeDiscriminator: "student")]
[JsonDerivedType(typeof(Teacher), typeDiscriminator: "teacher")]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "discriminator")]
internal abstract class Person { }

internal class Student : Person
{
    public decimal GPA { get; set; }
}

internal class Teacher : Person
{
    public required string Subject { get; set; }
}

[JsonDerivedType(typeof(PaintColor), typeDiscriminator: "paint")]
[JsonDerivedType(typeof(FabricColor), typeDiscriminator: "fabric")]
internal class Color
{
    public required string HexCode { get; set; }
}

internal class PaintColor : Color
{
    public bool IsMatte { get; set; }
}
internal class FabricColor : Color
{
    public required string Dye { get; set; }
}
