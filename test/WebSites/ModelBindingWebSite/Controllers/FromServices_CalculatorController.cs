// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ModelBindingWebSite.Controllers
{
    public class FromServices_CalculatorController : Controller
    {
        public int Calculate(CalculatorContext context)
        {
            return context.Calculator.Operation(context.Operator, context.Left, context.Right);
        }

        public int Add(int left, int right, [FromServices] ICalculator calculator)
        {
            return calculator.Operation('+', left, right);
        }

        public int CalculateWithPrecision(DefaultCalculatorContext context)
        {
            return context.Calculator.Operation(context.Operator, context.Left, context.Right);
        }
    }
}