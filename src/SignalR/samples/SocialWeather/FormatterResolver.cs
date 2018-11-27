// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace SocialWeather
{
    public class FormatterResolver
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly Dictionary<string, Dictionary<Type, Type>> _formatters
            = new Dictionary<string, Dictionary<Type, Type>>();

        public FormatterResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void AddFormatter<T, TFormatterType>(string formatType)
            where TFormatterType : IStreamFormatter<T>
        {
            Dictionary<Type, Type> typeFormatters;
            if (!_formatters.TryGetValue(formatType, out typeFormatters))
            {
                typeFormatters = _formatters[formatType] = new Dictionary<Type, Type>();
            }
            typeFormatters[typeof(T)] = typeof(TFormatterType);
        }

        public IStreamFormatter<T> GetFormatter<T>(string formatType)
        {
            Dictionary<Type, Type> typeFormatters;
            Type typeFormatterType;
            if (_formatters.TryGetValue(formatType, out typeFormatters) &&
                typeFormatters.TryGetValue(typeof(T), out typeFormatterType))
            {
                return (IStreamFormatter<T>)_serviceProvider.GetRequiredService(typeFormatterType);
            }

            return null;
        }
    }
}
