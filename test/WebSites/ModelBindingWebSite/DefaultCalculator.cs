// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
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
            _logger = loggerFactory.Create(typeof(DefaultCalculator).FullName);
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
                    _logger.WriteError("Unrecognized operator:" + @operator);
                    throw new InvalidOperationException();
            }
        }
    }
}