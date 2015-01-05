// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.WebApiCompatShim;
using Microsoft.Framework.DependencyInjection;
using Newtonsoft.Json;

namespace System.Web.Http
{
    [UseWebApiRoutes]
    [UseWebApiActionConventions]
    [UseWebApiParameterConventions]
    [UseWebApiOverloading]
    public abstract class ApiController : IDisposable
    {
        private HttpRequestMessage _request;

        /// <summary>
        /// Gets the action context.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the HTTP request message.
        /// </summary>
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

        /// <summary>
        /// Gets a factory used to generate URLs to other APIs.
        /// </summary>
        /// <remarks>The setter is intended for unit testing purposes only.</remarks>
        [Activate]
        public IUrlHelper Url { get; set; }

        /// <summary>
        /// Gets or sets the current principal associated with this request.
        /// </summary>
        public IPrincipal User
        {
            get
            {
                return Context?.User;
            }
        }

        /// <summary>
        /// Creates a <see cref="BadRequestResult"/> (400 Bad Request).
        /// </summary>
        /// <returns>A <see cref="BadRequestResult"/>.</returns>
        [NonAction]
        public virtual BadRequestResult BadRequest()
        {
            return new BadRequestResult();
        }

        /// <summary>
        /// Creates a <see cref="BadRequestErrorMessageResult"/> (400 Bad Request) with the specified error message.
        /// </summary>
        /// <param name="message">The user-visible error message.</param>
        /// <returns>A <see cref="BadRequestErrorMessageResult"/> with the specified error message.</returns>
        [NonAction]
        public virtual BadRequestErrorMessageResult BadRequest([NotNull] string message)
        {
            return new BadRequestErrorMessageResult(message);
        }

        /// <summary>
        /// Creates an <see cref="InvalidModelStateResult"/> (400 Bad Request) with the specified model state.
        /// </summary>
        /// <param name="modelState">The model state to include in the error.</param>
        /// <returns>An <see cref="InvalidModelStateResult"/> with the specified model state.</returns>
        [NonAction]
        public virtual InvalidModelStateResult BadRequest([NotNull] ModelStateDictionary modelState)
        {
            return new InvalidModelStateResult(modelState, includeErrorDetail: false);
        }

        /// <summary>Creates a <see cref="ConflictResult"/> (409 Conflict).</summary>
        /// <returns>A <see cref="ConflictResult"/>.</returns>
        [NonAction]
        public virtual ConflictResult Conflict()
        {
            return new ConflictResult();
        }

        /// <summary>
        /// Creates a <see cref="NegotiatedContentResult{T}"/> with the specified values.
        /// </summary>
        /// <typeparam name="T">The type of content in the entity body.</typeparam>
        /// <param name="statusCode">The HTTP status code for the response message.</param>
        /// <param name="value">The content value to negotiate and format in the entity body.</param>
        /// <returns>A <see cref="NegotiatedContentResult{T}"/> with the specified values.</returns>
        [NonAction]
        public virtual NegotiatedContentResult<T> Content<T>(HttpStatusCode statusCode, [NotNull] T value)
        {
            return new NegotiatedContentResult<T>(statusCode, value);
        }

        /// <summary>
        /// Creates a <see cref="CreatedResult"/> (201 Created) with the specified values.
        /// </summary>
        /// <param name="location">
        /// The location at which the content has been created. Must be a relative or absolute URL.
        /// </param>
        /// <param name="content">The content value to format in the entity body.</param>
        /// <returns>A <see cref="CreatedResult"/> with the specified values.</returns>
        [NonAction]
        public virtual CreatedResult Created([NotNull] string location, object content)
        {
            return new CreatedResult(location, content);
        }

        /// <summary>
        /// Creates a <see cref="CreatedResult"/> (201 Created) with the specified values.
        /// </summary>
        /// <param name="location">The location at which the content has been created.</param>
        /// <param name="content">The content value to format in the entity body.</param>
        /// <returns>A <see cref="CreatedResult"/> with the specified values.</returns>
        [NonAction]
        public virtual CreatedResult Created([NotNull] Uri uri, object content)
        {
            string location;
            if (uri.IsAbsoluteUri)
            {
                location = uri.AbsoluteUri;
            }
            else
            {
                location = uri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
            }
            return Created(location, content);
        }

        /// <summary>
        /// Creates a <see cref="CreatedAtRouteResult"/> (201 Created) with the specified values.
        /// </summary>
        /// <param name="routeName">The name of the route to use for generating the URL.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="content">The content value to format in the entity body.</param>
        /// <returns>A <see cref="CreatedAtRouteResult"/> with the specified values.</returns>
        [NonAction]
        public virtual CreatedAtRouteResult CreatedAtRoute(
            [NotNull] string routeName,
            object routeValues,
            object content)
        {
            return new CreatedAtRouteResult(routeName, routeValues, content);
        }

        /// <summary
        /// >Creates an <see cref="InternalServerErrorResult"/> (500 Internal Server Error).
        /// </summary>
        /// <returns>A <see cref="InternalServerErrorResult"/>.</returns>
        [NonAction]
        public virtual InternalServerErrorResult InternalServerError()
        {
            return new InternalServerErrorResult();
        }

        /// <summary>
        /// Creates an <see cref="ExceptionResult"/> (500 Internal Server Error) with the specified exception.
        /// </summary>
        /// <param name="exception">The exception to include in the error.</param>
        /// <returns>An <see cref="ExceptionResult"/> with the specified exception.</returns>
        [NonAction]
        public virtual ExceptionResult InternalServerError([NotNull] Exception exception)
        {
            return new ExceptionResult(exception, includeErrorDetail: false);
        }

        /// <summary>
        /// Creates an <see cref="JsonResult"/> (200 OK) with the specified value.
        /// </summary>
        /// <typeparam name="T">The type of content in the entity body.</typeparam>
        /// <param name="content">The content value to serialize in the entity body.</param>
        /// <returns>A <see cref="JsonResult"/> with the specified value.</returns>
        [NonAction]
        public virtual JsonResult Json<T>([NotNull] T content)
        {
            return new JsonResult(content);
        }

        /// <summary>
        /// Creates an <see cref="JsonResult"/> (200 OK) with the specified values.
        /// </summary>
        /// <typeparam name="T">The type of content in the entity body.</typeparam>
        /// <param name="content">The content value to serialize in the entity body.</param>
        /// <param name="serializerSettings">The serializer settings.</param>
        /// <returns>A <see cref="JsonResult"/> with the specified values.</returns>
        [NonAction]
        public virtual JsonResult Json<T>([NotNull] T content, [NotNull] JsonSerializerSettings serializerSettings)
        {
            var formatter = new JsonOutputFormatter()
            {
                SerializerSettings = serializerSettings,
            };

            return new JsonResult(content, formatter);
        }

        /// <summary>
        /// Creates an <see cref="JsonResult"/> (200 OK) with the specified values.
        /// </summary>
        /// <typeparam name="T">The type of content in the entity body.</typeparam>
        /// <param name="content">The content value to serialize in the entity body.</param>
        /// <param name="serializerSettings">The serializer settings.</param>
        /// <param name="encoding">The content encoding.</param>
        /// <returns>A <see cref="JsonResult"/> with the specified values.</returns>
        [NonAction]
        public virtual JsonResult Json<T>(
            [NotNull] T content,
            [NotNull] JsonSerializerSettings serializerSettings,
            [NotNull] Encoding encoding)
        {
            var formatter = new JsonOutputFormatter()
            {
                SerializerSettings = serializerSettings,
            };

            formatter.SupportedEncodings.Clear();
            formatter.SupportedEncodings.Add(encoding);

            return new JsonResult(content, formatter);
        }

        /// <summary>
        /// Creates an <see cref="HttpNotFoundResult"/> (404 Not Found).
        /// </summary>
        /// <returns>A <see cref="HttpNotFoundResult"/>.</returns>
        [NonAction]
        public virtual HttpNotFoundResult NotFound()
        {
            return new HttpNotFoundResult();
        }

        /// <summary>
        /// Creates an <see cref="OkResult"/> (200 OK).
        /// </summary>
        /// <returns>An <see cref="OkResult"/>.</returns>
        [NonAction]
        public virtual OkResult Ok()
        {
            return new OkResult();
        }

        /// <summary>
        /// Creates an <see cref="OkNegotiatedContentResult{T}"/> (200 OK) with the specified values.
        /// </summary>
        /// <typeparam name="T">The type of content in the entity body.</typeparam>
        /// <param name="content">The content value to negotiate and format in the entity body.</param>
        /// <returns>An <see cref="OkNegotiatedContentResult{T}"/> with the specified values.</returns>
        [NonAction]
        public virtual OkNegotiatedContentResult<T> Ok<T>([NotNull] T content)
        {
            return new OkNegotiatedContentResult<T>(content);
        }

        /// <summary>
        /// Creates a <see cref="RedirectResult"/> (302 Found) with the specified value.
        /// </summary>
        /// <param name="location">The location to which to redirect.</param>
        /// <returns>A <see cref="RedirectResult"/> with the specified value.</returns>
        [NonAction]
        public virtual RedirectResult Redirect([NotNull] string location)
        {
            // This is how redirect was implemented in legacy webapi - string URIs are assumed to be absolute.
            return Redirect(new Uri(location));
        }

        /// <summary>
        /// Creates a <see cref="RedirectResult"/> (302 Found) with the specified value.
        /// </summary>
        /// <param name="location">The location to which to redirect.</param>
        /// <returns>A <see cref="RedirectResult"/> with the specified value.</returns>
        [NonAction]
        public virtual RedirectResult Redirect([NotNull] Uri location)
        {
            string uri;
            if (location.IsAbsoluteUri)
            {
                uri = location.AbsoluteUri;
            }
            else
            {
                uri = location.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
            }

            return new RedirectResult(uri);
        }

        /// <summary>
        /// Creates a <see cref="RedirectToRouteResult"/> (302 Found) with the specified values.
        /// </summary>
        /// <param name="routeName">The name of the route to use for generating the URL.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <returns>A <see cref="RedirectToRouteResult"/> with the specified values.</returns>
        [NonAction]
        public virtual RedirectToRouteResult RedirectToRoute([NotNull] string routeName, [NotNull] object routeValues)
        {
            return new RedirectToRouteResult(Url, routeName, routeValues);
        }

        /// <summary>
        /// Creates a <see cref="ResponseMessageResult"/> with the specified response.
        /// </summary>
        /// <param name="response">The HTTP response message.</param>
        /// <returns>A <see cref="ResponseMessageResult"/> for the specified response.</returns>
        [NonAction]
        public virtual ResponseMessageResult ResponseMessage([NotNull] HttpResponseMessage response)
        {
            return new ResponseMessageResult(response);
        }

        /// <summary>
        /// Creates a <see cref="HttpStatusCodeResult"/> with the specified status code.
        /// </summary>
        /// <param name="status">The HTTP status code for the response message</param>
        /// <returns>A <see cref="HttpStatusCodeResult"/> with the specified status code.</returns>
        [NonAction]
        public virtual HttpStatusCodeResult StatusCode(HttpStatusCode status)
        {
            return new HttpStatusCodeResult((int)status);
        }

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
            var bodyValidationExcludeFiltersProvider = Context.RequestServices
                                                              .GetRequiredService<IValidationExcludeFiltersProvider>();
            var validator = Context.RequestServices.GetRequiredService<IBodyModelValidator>();
            var metadataProvider = Context.RequestServices.GetRequiredService<IModelMetadataProvider>();
            var modelMetadata = metadataProvider.GetMetadataForType(() => entity, typeof(TEntity));
            var validatorProvider = Context.RequestServices.GetRequiredService<ICompositeModelValidatorProvider>();
            var modelValidationContext = new ModelValidationContext(
                metadataProvider,
                validatorProvider,
                ModelState,
                modelMetadata,
                containerMetadata: null,
                excludeFromValidationFilters: bodyValidationExcludeFiltersProvider.ExcludeFilters);
            validator.Validate(modelValidationContext, keyPrefix);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
