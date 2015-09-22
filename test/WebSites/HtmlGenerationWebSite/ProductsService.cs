// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using Microsoft.Framework.Primitives;

namespace HtmlGenerationWebSite
{
    public class ProductsService
    {
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public string GetProducts(string category, out IChangeToken changeToken)
        {
            var token = _tokenSource.IsCancellationRequested ?
                CancellationToken.None : _tokenSource.Token;
            changeToken = new CancellationChangeToken(token);
            if (category == "Books")
            {
                return "Book1, Book2";
            }
            else
            {
                return "Laptops";
            }
        }

        public void UpdateProducts()
        {
            _tokenSource.Cancel();
        }
    }
}