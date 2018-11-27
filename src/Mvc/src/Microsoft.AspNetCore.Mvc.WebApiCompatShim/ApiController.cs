// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace System.Web.Http
{
    [UseWebApiRoutes]
    [UseWebApiActionConventions]
    [UseWebApiParameterConventions]
    [UseWebApiOverloading]
    [Controller]
    public abstract class ApiController : IDisposable
    {
        private ControllerContext _controllerContext;
        private HttpRequestMessage _request;
        private IModelMetadataProvider _metadataProvider;
        private IObjectModelValidator _objectValidator;
        private IUrlHelper _urlHelper;

        /// <summary>
        /// Gets the <see cref="Microsoft.AspNetCore.Mvc.ActionContext"/>.
        /// </summary>
        public ActionContext ActionContext => ControllerContext;

        /// <summary>
        /// Gets or sets the <see cref="ControllerContext"/>.
        /// </summary>
        /// <remarks>The setter is intended for unit testing purposes only.</remarks>
        [ControllerContext]
        public ControllerContext ControllerContext
        {
            get
            {
                if (_controllerContext == null)
                {
                    _controllerContext = new ControllerContext();
                }

                return _controllerContext;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _controllerContext = value;
            }
        }

        /// <summary>
        /// Gets the http context.
        /// </summary>
        public HttpContext Context
        {
            get
            {
                return ControllerContext.HttpContext;
            }
        }

        /// <summary>
        /// Gets the <see cref="IModelMetadataProvider"/>.
        /// </summary>
        /// <remarks>The setter is intended for unit testing purposes only.</remarks>
        public IModelMetadataProvider MetadataProvider
        {
            get
            {
                if (_metadataProvider == null)
                {
                    _metadataProvider = Context?.RequestServices.GetRequiredService<IModelMetadataProvider>();
                }

                return _metadataProvider;
            }
            set
            {
                _metadataProvider = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="IObjectModelValidator"/>.
        /// </summary>
        public IObjectModelValidator ObjectValidator
        {
            get
            {
                if (_objectValidator == null)
                {
                    _objectValidator = Context?.RequestServices.GetRequiredService<IObjectModelValidator>();
                }

                return _objectValidator;
            }
            set
            {
                _objectValidator = value;
            }
        }

        /// <summary>
        /// Gets model state after the model binding process. This ModelState will be empty before model binding
        /// happens.
        /// </summary>
        public ModelStateDictionary ModelState
        {
            get
            {
                return ControllerContext.ModelState;
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
                    _request = ControllerContext.HttpContext.GetHttpRequestMessage();
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
        public IUrlHelper Url
        {
            get
            {
                if (_urlHelper == null)
                {
                    var factory = Context?.RequestServices.GetRequiredService<IUrlHelperFactory>();
                    _urlHelper = factory?.GetUrlHelper(ActionContext);
                }

                return _urlHelper;
            }
            set
            {
                _urlHelper = value;
            }
        }

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
        public virtual BadRequestErrorMessageResult BadRequest(string message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return new BadRequestErrorMessageResult(message);
        }

        /// <summary>
        /// Creates an <see cref="InvalidModelStateResult"/> (400 Bad Request) with the specified model state.
        /// </summary>
        /// <param name="modelState">The model state to include in the error.</param>
        /// <returns>An <see cref="InvalidModelStateResult"/> with the specified model state.</returns>
        [NonAction]
        public virtual InvalidModelStateResult BadRequest(ModelStateDictionary modelState)
        {
            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

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
        public virtual NegotiatedContentResult<T> Content<T>(HttpStatusCode statusCode, T value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

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
        public virtual CreatedResult Created(string location, object content)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            return new CreatedResult(location, content);
        }

        /// <summary>
        /// Creates a <see cref="CreatedResult"/> (201 Created) with the specified values.
        /// </summary>
        /// <param name="uri">The location at which the content has been created.</param>
        /// <param name="content">The content value to format in the entity body.</param>
        /// <returns>A <see cref="CreatedResult"/> with the specified values.</returns>
        [NonAction]
        public virtual CreatedResult Created(Uri uri, object content)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

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
            string routeName,
            object routeValues,
            object content)
        {
            if (routeName == null)
            {
                throw new ArgumentNullException(nameof(routeName));
            }

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
        public virtual ExceptionResult InternalServerError(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return new ExceptionResult(exception, includeErrorDetail: false);
        }

        /// <summary>
        /// Creates an <see cref="JsonResult"/> (200 OK) with the specified value.
        /// </summary>
        /// <typeparam name="T">The type of content in the entity body.</typeparam>
        /// <param name="content">The content value to serialize in the entity body.</param>
        /// <returns>A <see cref="JsonResult"/> with the specified value.</returns>
        [NonAction]
        public virtual JsonResult Json<T>(T content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

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
        public virtual JsonResult Json<T>(T content, JsonSerializerSettings serializerSettings)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (serializerSettings == null)
            {
                throw new ArgumentNullException(nameof(serializerSettings));
            }

            return new JsonResult(content, serializerSettings);
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
            T content,
            JsonSerializerSettings serializerSettings,
            Encoding encoding)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (serializerSettings == null)
            {
                throw new ArgumentNullException(nameof(serializerSettings));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            var result = new JsonResult(content, serializerSettings);
            result.ContentType = $"application/json; charset={encoding.WebName}";

            return result;
        }

        /// <summary>
        /// Creates an <see cref="NotFoundResult"/> (404 Not Found).
        /// </summary>
        /// <returns>A <see cref="NotFoundResult"/>.</returns>
        [NonAction]
        public virtual NotFoundResult NotFound()
        {
            return new NotFoundResult();
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
        /// Creates an <see cref="OkObjectResult"/> (200 OK) with the specified values.
        /// </summary>
        /// <typeparam name="T">The type of content in the entity body.</typeparam>
        /// <param name="content">The content value to negotiate and format in the entity body.</param>
        /// <returns>An <see cref="OkObjectResult"/> with the specified values.</returns>
        [NonAction]
        public virtual OkObjectResult Ok<T>(T content)
        {
            return new OkObjectResult(content);
        }

        /// <summary>
        /// Creates a <see cref="RedirectResult"/> (302 Found) with the specified value.
        /// </summary>
        /// <param name="location">The location to which to redirect.</param>
        /// <returns>A <see cref="RedirectResult"/> with the specified value.</returns>
        [NonAction]
        public virtual RedirectResult Redirect(string location)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            // This is how redirect was implemented in legacy webapi - string URIs are assumed to be absolute.
            return Redirect(new Uri(location));
        }

        /// <summary>
        /// Creates a <see cref="RedirectResult"/> (302 Found) with the specified value.
        /// </summary>
        /// <param name="location">The location to which to redirect.</param>
        /// <returns>A <see cref="RedirectResult"/> with the specified value.</returns>
        [NonAction]
        public virtual RedirectResult Redirect(Uri location)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

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
        public virtual RedirectToRouteResult RedirectToRoute(string routeName, object routeValues)
        {
            if (routeName == null)
            {
                throw new ArgumentNullException(nameof(routeName));
            }

            if (routeValues == null)
            {
                throw new ArgumentNullException(nameof(routeValues));
            }

            return new RedirectToRouteResult(routeName, routeValues)
            {
                UrlHelper = Url,
            };
        }

        /// <summary>
        /// Creates a <see cref="ResponseMessageResult"/> with the specified response.
        /// </summary>
        /// <param name="response">The HTTP response message.</param>
        /// <returns>A <see cref="ResponseMessageResult"/> for the specified response.</returns>
        [NonAction]
        public virtual ResponseMessageResult ResponseMessage(HttpResponseMessage response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            return new ResponseMessageResult(response);
        }

        /// <summary>
        /// Creates a <see cref="StatusCodeResult"/> with the specified status code.
        /// </summary>
        /// <param name="status">The HTTP status code for the response message</param>
        /// <returns>A <see cref="StatusCodeResult"/> with the specified status code.</returns>
        [NonAction]
        public virtual StatusCodeResult StatusCode(HttpStatusCode status)
        {
            return new StatusCodeResult((int)status);
        }

        /// <inheritdoc />
        public void Dispose() => Dispose(disposing: true);

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
            ObjectValidator.Validate(
                ControllerContext,
                validationState: null,
                prefix: keyPrefix,
                model: entity);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
