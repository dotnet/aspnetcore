// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace ModelBindingWebSite.Controllers
{
    [Route("ModelBinderAttribute_Product/[action]")]
    public class ModelBinderAttribute_ProductController : Controller
    {
        public string GetBinderType_UseModelBinderOnType(
            [ModelBinder(Name = "customPrefix")] ProductWithBinderOnType model)
        {
            return model.BinderType.FullName;
        }

        public string GetBinderType_UseModelBinder(
            [ModelBinder(BinderType = typeof(ProductModelBinder))] Product model)
        {
            return model.BinderType.FullName;
        }

        public string GetBinderType_UseModelBinderOnProperty(Order order)
        {
            return order.Product.BinderType.FullName;
        }

        public string ModelBinderAttribute_UseModelBinderOnEnum(OrderStatus status)
        {
            return status.ToString();
        }

        public class Product
        {
            public int ProductId { get; set; }

            // Will be set by the binder
            public Type BinderType { get; set; }
        }

        [ModelBinder(BinderType = typeof(ProductModelBinder))]
        public class ProductWithBinderOnType :  Product
        {
        }

        public class Order
        {
            [ModelBinder(BinderType = typeof(ProductModelBinder))]
            public Product Product { get; set; }
        }

        [ModelBinder(BinderType = typeof(OrderStatusBinder))]
        public enum OrderStatus
        {
            StatusOutOfStock,
            StatusShipped,
            StatusRecieved,
        }

        private class OrderStatusBinder : IModelBinder
        {
            public Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
            {
                if (typeof(OrderStatus).IsAssignableFrom(bindingContext.ModelType))
                {
                    var request = bindingContext.OperationBindingContext.HttpContext.Request;

                    // Doing something slightly different here to make sure we don't get accidentally bound
                    // by the type converter binder.
                    OrderStatus model;
                    if (Enum.TryParse<OrderStatus>("Status" + request.Query["status"], out model))
                    {
                        return ModelBindingResult.SuccessAsync("status", model);
                    }
                    
                    return ModelBindingResult.FailedAsync("status");
                }

                return ModelBindingResult.NoResultAsync;
            }
        }

        private class ProductModelBinder : IModelBinder
        {
            public Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
            {
                if (typeof(Product).IsAssignableFrom(bindingContext.ModelType))
                {
                    var model = (Product)Activator.CreateInstance(bindingContext.ModelType);

                    model.BinderType = GetType();

                    var key =
                        string.IsNullOrEmpty(bindingContext.ModelName) ?
                        "productId" :
                        bindingContext.ModelName + "." + "productId";
                    
                    var value = bindingContext.ValueProvider.GetValue(key);
                    model.ProductId = value.ConvertTo<int>();

                    return ModelBindingResult.SuccessAsync(key, model);
                }

                return ModelBindingResult.NoResultAsync;
            }
        }
    }
}