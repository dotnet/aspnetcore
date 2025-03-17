// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

// Type hierarchy for validating abstract base type with string discriminators.
[JsonDerivedType(typeof(Triangle), typeDiscriminator: "triangle")]
[JsonDerivedType(typeof(Square), typeDiscriminator: "square")]
public abstract class Shape
{
    public string Color { get; set; } = string.Empty;
    public int Sides { get; set; }
}

public class Triangle : Shape
{
    public double Hypotenuse { get; set; }
}
public class Square : Shape
{
    public double Area { get; set; }
}

// Type hierarchy for validating abstract base type with integer discriminators.
[JsonDerivedType(typeof(WeatherForecastWithCity), 0)]
[JsonDerivedType(typeof(WeatherForecastWithTimeSeries), 1)]
[JsonDerivedType(typeof(WeatherForecastWithLocalNews), 2)]
public abstract class WeatherForecastBase { }

public class WeatherForecastWithCity : WeatherForecastBase
{
    public required string City { get; set; }
}

public class WeatherForecastWithTimeSeries : WeatherForecastBase
{
    public DateTimeOffset Date { get; set; }
    public int TemperatureC { get; set; }
    public required string Summary { get; set; }
}

public class WeatherForecastWithLocalNews : WeatherForecastBase
{
    public required string News { get; set; }
}

// Type hierarchy for validating custom discriminator property name.
[JsonDerivedType(typeof(Student), typeDiscriminator: "student")]
[JsonDerivedType(typeof(Teacher), typeDiscriminator: "teacher")]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "discriminator")]
public abstract class Person { }

public class Student : Person
{
    public decimal GPA { get; set; }
}

public class Teacher : Person
{
    public required string Subject { get; set; }
}

// Type hierarchy for validating non-abstract base-type that is not explicitly
// registered as its own derived type. This should produce an OpenAPI schema with
// `anyOf` set and no `discriminator` property.
[JsonDerivedType(typeof(PaintColor), typeDiscriminator: "paint")]
[JsonDerivedType(typeof(FabricColor), typeDiscriminator: "fabric")]
public class Color
{
    public required string HexCode { get; set; }
}

public class PaintColor : Color
{
    public bool IsMatte { get; set; }
}
public class FabricColor : Color
{
    public required string Dye { get; set; }
}

// Type hierarchy for validating non-abstract base type that is
// explicitly defined as its own derived type. This should produce an OpenAPI
// with `discriminator` property.
[JsonDerivedType(typeof(Cat), typeDiscriminator: "cat")]
[JsonDerivedType(typeof(Dog), typeDiscriminator: "dog")]
[JsonDerivedType(typeof(Pet), typeDiscriminator: "pet")]
public class Pet
{
    public required string Name { get; set; }
}

public class Cat : Pet
{
    public bool IsKitten { get; set; }
}
public class Dog : Pet
{
    public required string Breed { get; set; }
}

// Type hierarchy for validating derived types without discriminators
// set. This should produce an OpenAPI schema with `anyOf` set and no
// `discriminator` mapping.
[JsonDerivedType(typeof(Animal))]
[JsonDerivedType(typeof(Plant))]
public class Organism
{
    public required string Name { get; set; }
}

public class Animal : Organism
{
    public int Legs { get; set; }
}

public class Plant : Organism
{
    public bool IsEdible { get; set; }
}

// Type hierarchy for validating polymorphic types with self-references.
[JsonDerivedType(typeof(Manager), typeDiscriminator: "manager")]
[JsonDerivedType(typeof(Employee), typeDiscriminator: "employee")]
public class Employee
{
    public required string Name { get; set; }
    public required Employee Manager { get; set; }
}

public class Manager : Employee
{
    public required string Department { get; set; }
}

