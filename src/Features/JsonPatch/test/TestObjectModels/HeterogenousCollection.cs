// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.JsonPatch;

public abstract class Shape
{
    public string ShapeProperty { get; set; }
}

public class Circle : Shape
{
    public string CircleProperty { get; set; }
}

public class Rectangle : Shape
{
    public string RectangleProperty { get; set; }
}

public class Canvas
{
    public IList<Shape> Items { get; set; }
}
