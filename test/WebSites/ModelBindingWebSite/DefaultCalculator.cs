// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Logging;

namespace ModelBindingWebSite
{
    public class DefaultCalculator : ICalculator
    {
        private ILogger _logger;

        public DefaultCalculator(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(typeof(DefaultCalculator).FullName);
        }

        public int Operation(char @operator, int left, int right)
        {
            switch (@operator)
            {
                case '+': return left + right;
                case '-': return left - right;
                case '*': return left * right;
                case '/': return left / right;
                default:
                    _logger.LogError("Unrecognized operator:" + @operator);
                    throw new InvalidOperationException();
            }
        }
    }
}