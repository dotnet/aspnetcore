// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.JsonPatch
{
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
}
