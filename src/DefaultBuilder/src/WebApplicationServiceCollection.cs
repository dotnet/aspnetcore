// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore
{
    internal sealed class WebApplicationServiceCollection : IServiceCollection
    {
        private IServiceCollection _services = new ServiceCollection();

        public ServiceDescriptor this[int index] { get => _services[index]; set => _services[index] = value; }

        public int Count => _services.Count;

        public bool IsReadOnly => _services.IsReadOnly;

        public IServiceCollection InnerCollection { get => _services; set => _services = value; }

        public void Add(ServiceDescriptor item)
        {
            _services.Add(item);
        }

        public void Clear()
        {
            _services.Clear();
        }

        public bool Contains(ServiceDescriptor item)
        {
            return _services.Contains(item);
        }

        public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
        {
            _services.CopyTo(array, arrayIndex);
        }

        public IEnumerator<ServiceDescriptor> GetEnumerator()
        {
            return _services.GetEnumerator();
        }

        public int IndexOf(ServiceDescriptor item)
        {
            return _services.IndexOf(item);
        }

        public void Insert(int index, ServiceDescriptor item)
        {
            _services.Insert(index, item);
        }

        public bool Remove(ServiceDescriptor item)
        {
            return _services.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _services.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
