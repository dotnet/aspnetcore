// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Security.Principal;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.WebApiCompatShim;
using Microsoft.Framework.DependencyInjection;

namespace System.Web.Http
{
    [UseWebApiActionConventions]
    [UseWebApiOverloading]
    public abstract class ApiController : IDisposable
    {
        private HttpRequestMessage _request;

        /// <summary>Gets the action context.</summary>
        /// <remarks>The setter is intended for unit testing purposes only.</remarks>
        [Activate]
        public ActionContext ActionContext { get; set; }

        /// <summary>
        /// Gets the http context.
        /// </summary>
        public HttpContext Context
        {
            get
            {
                return ActionContext?.HttpContext;
            }
        }

        /// <summary>
        /// Gets model state after the model binding process. This ModelState will be empty before model binding happens.
        /// </summary>
        public ModelStateDictionary ModelState
        {
            get
            {
                return ActionContext?.ModelState;
            }
        }

        /// <summary>Gets or sets the HTTP request message.</summary>
        /// <remarks>The setter is intended for unit testing purposes only.</remarks>
        public HttpRequestMessage Request
        {
            get
            {
                if (_request == null && ActionContext != null)
                {
                    _request = ActionContext.HttpContext.GetHttpRequestMessage();
                }

                return _request;
            }
            set
            {
                _request = value;
            }
        }

        /// <summary>Gets a factory used to generate URLs to other APIs.</summary>
        /// <remarks>The setter is intended for unit testing purposes only.</remarks>
        [Activate]
        public IUrlHelper Url { get; set; }

        /// <summary>Gets or sets the current principal associated with this request.</summary>
        public IPrincipal User
        {
            get
            {
                return Context?.User;
            }
        }

        [NonAction]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Validates the given entity and adds the validation errors to the <see cref="ApiController.ModelState"/>
        /// under an empty prefix.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity to be validated.</typeparam>
        /// <param name="entity">The entity being validated.</param>
        public void Validate<TEntity>(TEntity entity)
        {
            Validate(entity, keyPrefix: string.Empty);
        }

        /// <summary>
        /// Validates the given entity and adds the validation errors to the <see cref="ApiController.ModelState"/>.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity to be validated.</typeparam>
        /// <param name="entity">The entity being validated.</param>
        /// <param name="keyPrefix">
        /// The key prefix under which the model state errors would be added in the 
        /// <see cref="ApiController.ModelState"/>.
        /// </param>
        public void Validate<TEntity>(TEntity entity, string keyPrefix)
        {
            var validator = Context.RequestServices.GetService<IBodyModelValidator>();
            var metadataProvider = Context.RequestServices.GetService<IModelMetadataProvider>();
            var modelMetadata = metadataProvider.GetMetadataForType(() => entity, typeof(TEntity));
            var validatorProvider = Context.RequestServices.GetService<ICompositeModelValidatorProvider>();
            var modelValidationContext = new ModelValidationContext(metadataProvider,
                                                                    validatorProvider,
                                                                    ModelState,
                                                                    modelMetadata,
                                                                    containerMetadata: null);
            validator.Validate(modelValidationContext, keyPrefix);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
