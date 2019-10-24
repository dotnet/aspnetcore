// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// A base class for an MVC controller without view support.
    /// </summary>
    [Controller]
    public abstract class ControllerBase
    {
        private ControllerContext _controllerContext;
        private IModelMetadataProvider _metadataProvider;
        private IModelBinderFactory _modelBinderFactory;
        private IObjectModelValidator _objectValidator;
        private IUrlHelper _url;
        private ProblemDetailsFactory _problemDetailsFactory;

        /// <summary>
        /// Gets the <see cref="Http.HttpContext"/> for the executing action.
        /// </summary>
        public HttpContext HttpContext => ControllerContext.HttpContext;

        /// <summary>
        /// Gets the <see cref="HttpRequest"/> for the executing action.
        /// </summary>
        public HttpRequest Request => HttpContext?.Request;

        /// <summary>
        /// Gets the <see cref="HttpResponse"/> for the executing action.
        /// </summary>
        public HttpResponse Response => HttpContext?.Response;

        /// <summary>
        /// Gets the <see cref="AspNetCore.Routing.RouteData"/> for the executing action.
        /// </summary>
        public RouteData RouteData => ControllerContext.RouteData;

        /// <summary>
        /// Gets the <see cref="ModelStateDictionary"/> that contains the state of the model and of model-binding validation.
        /// </summary>
        public ModelStateDictionary ModelState => ControllerContext.ModelState;

        /// <summary>
        /// Gets or sets the <see cref="Mvc.ControllerContext"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="Controllers.IControllerActivator"/> activates this property while activating controllers.
        /// If user code directly instantiates a controller, the getter returns an empty
        /// <see cref="Mvc.ControllerContext"/>.
        /// </remarks>
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
        /// Gets or sets the <see cref="IModelMetadataProvider"/>.
        /// </summary>
        public IModelMetadataProvider MetadataProvider
        {
            get
            {
                if (_metadataProvider == null)
                {
                    _metadataProvider = HttpContext?.RequestServices?.GetRequiredService<IModelMetadataProvider>();
                }

                return _metadataProvider;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _metadataProvider = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="IModelBinderFactory"/>.
        /// </summary>
        public IModelBinderFactory ModelBinderFactory
        {
            get
            {
                if (_modelBinderFactory == null)
                {
                    _modelBinderFactory = HttpContext?.RequestServices?.GetRequiredService<IModelBinderFactory>();
                }

                return _modelBinderFactory;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _modelBinderFactory = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="IUrlHelper"/>.
        /// </summary>
        public IUrlHelper Url
        {
            get
            {
                if (_url == null)
                {
                    var factory = HttpContext?.RequestServices?.GetRequiredService<IUrlHelperFactory>();
                    _url = factory?.GetUrlHelper(ControllerContext);
                }

                return _url;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _url = value;
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
                    _objectValidator = HttpContext?.RequestServices?.GetRequiredService<IObjectModelValidator>();
                }

                return _objectValidator;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _objectValidator = value;
            }
        }

        public ProblemDetailsFactory ProblemDetailsFactory
        {
            get
            {
                if (_problemDetailsFactory == null)
                {
                    _problemDetailsFactory = HttpContext?.RequestServices?.GetRequiredService<ProblemDetailsFactory>();
                }

                return _problemDetailsFactory;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _problemDetailsFactory = value;
            }
        }

        /// <summary>
        /// Gets the <see cref="ClaimsPrincipal"/> for user associated with the executing action.
        /// </summary>
        public ClaimsPrincipal User => HttpContext?.User;

        /// <summary>
        /// Creates a <see cref="StatusCodeResult"/> object by specifying a <paramref name="statusCode"/>.
        /// </summary>
        /// <param name="statusCode">The status code to set on the response.</param>
        /// <returns>The created <see cref="StatusCodeResult"/> object for the response.</returns>
        [NonAction]
        public virtual StatusCodeResult StatusCode([ActionResultStatusCode] int statusCode)
            => new StatusCodeResult(statusCode);

        /// <summary>
        /// Creates a <see cref="ObjectResult"/> object by specifying a <paramref name="statusCode"/> and <paramref name="value"/>
        /// </summary>
        /// <param name="statusCode">The status code to set on the response.</param>
        /// <param name="value">The value to set on the <see cref="ObjectResult"/>.</param>
        /// <returns>The created <see cref="ObjectResult"/> object for the response.</returns>
        [NonAction]
        public virtual ObjectResult StatusCode([ActionResultStatusCode] int statusCode, [ActionResultObjectValue] object value)
        {
            return new ObjectResult(value)
            {
                StatusCode = statusCode
            };
        }

        /// <summary>
        /// Creates a <see cref="ContentResult"/> object by specifying a <paramref name="content"/> string.
        /// </summary>
        /// <param name="content">The content to write to the response.</param>
        /// <returns>The created <see cref="ContentResult"/> object for the response.</returns>
        [NonAction]
        public virtual ContentResult Content(string content)
            => Content(content, (MediaTypeHeaderValue)null);

        /// <summary>
        /// Creates a <see cref="ContentResult"/> object by specifying a
        /// <paramref name="content"/> string and a content type.
        /// </summary>
        /// <param name="content">The content to write to the response.</param>
        /// <param name="contentType">The content type (MIME type).</param>
        /// <returns>The created <see cref="ContentResult"/> object for the response.</returns>
        [NonAction]
        public virtual ContentResult Content(string content, string contentType)
            => Content(content, MediaTypeHeaderValue.Parse(contentType));

        /// <summary>
        /// Creates a <see cref="ContentResult"/> object by specifying a
        /// <paramref name="content"/> string, a <paramref name="contentType"/>, and <paramref name="contentEncoding"/>.
        /// </summary>
        /// <param name="content">The content to write to the response.</param>
        /// <param name="contentType">The content type (MIME type).</param>
        /// <param name="contentEncoding">The content encoding.</param>
        /// <returns>The created <see cref="ContentResult"/> object for the response.</returns>
        /// <remarks>
        /// If encoding is provided by both the 'charset' and the <paramref name="contentEncoding"/> parameters, then
        /// the <paramref name="contentEncoding"/> parameter is chosen as the final encoding.
        /// </remarks>
        [NonAction]
        public virtual ContentResult Content(string content, string contentType, Encoding contentEncoding)
        {
            var mediaTypeHeaderValue = MediaTypeHeaderValue.Parse(contentType);
            mediaTypeHeaderValue.Encoding = contentEncoding ?? mediaTypeHeaderValue.Encoding;
            return Content(content, mediaTypeHeaderValue);
        }

        /// <summary>
        /// Creates a <see cref="ContentResult"/> object by specifying a
        /// <paramref name="content"/> string and a <paramref name="contentType"/>.
        /// </summary>
        /// <param name="content">The content to write to the response.</param>
        /// <param name="contentType">The content type (MIME type).</param>
        /// <returns>The created <see cref="ContentResult"/> object for the response.</returns>
        [NonAction]
        public virtual ContentResult Content(string content, MediaTypeHeaderValue contentType)
        {
            return new ContentResult
            {
                Content = content,
                ContentType = contentType?.ToString()
            };
        }

        /// <summary>
        /// Creates a <see cref="NoContentResult"/> object that produces an empty
        /// <see cref="StatusCodes.Status204NoContent"/> response.
        /// </summary>
        /// <returns>The created <see cref="NoContentResult"/> object for the response.</returns>
        [NonAction]
        public virtual NoContentResult NoContent()
            => new NoContentResult();

        /// <summary>
        /// Creates a <see cref="OkResult"/> object that produces an empty <see cref="StatusCodes.Status200OK"/> response.
        /// </summary>
        /// <returns>The created <see cref="OkResult"/> for the response.</returns>
        [NonAction]
        public virtual OkResult Ok()
            => new OkResult();

        /// <summary>
        /// Creates an <see cref="OkObjectResult"/> object that produces an <see cref="StatusCodes.Status200OK"/> response.
        /// </summary>
        /// <param name="value">The content value to format in the entity body.</param>
        /// <returns>The created <see cref="OkObjectResult"/> for the response.</returns>
        [NonAction]
        public virtual OkObjectResult Ok([ActionResultObjectValue] object value)
            => new OkObjectResult(value);

        #region RedirectResult variants
        /// <summary>
        /// Creates a <see cref="RedirectResult"/> object that redirects (<see cref="StatusCodes.Status302Found"/>)
        /// to the specified <paramref name="url"/>.
        /// </summary>
        /// <param name="url">The URL to redirect to.</param>
        /// <returns>The created <see cref="RedirectResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectResult Redirect(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(url));
            }

            return new RedirectResult(url);
        }

        /// <summary>
        /// Creates a <see cref="RedirectResult"/> object with <see cref="RedirectResult.Permanent"/> set to true
        /// (<see cref="StatusCodes.Status301MovedPermanently"/>) using the specified <paramref name="url"/>.
        /// </summary>
        /// <param name="url">The URL to redirect to.</param>
        /// <returns>The created <see cref="RedirectResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectResult RedirectPermanent(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(url));
            }

            return new RedirectResult(url, permanent: true);
        }

        /// <summary>
        /// Creates a <see cref="RedirectResult"/> object with <see cref="RedirectResult.Permanent"/> set to false
        /// and <see cref="RedirectResult.PreserveMethod"/> set to true (<see cref="StatusCodes.Status307TemporaryRedirect"/>) 
        /// using the specified <paramref name="url"/>.
        /// </summary>
        /// <param name="url">The URL to redirect to.</param>
        /// <returns>The created <see cref="RedirectResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectResult RedirectPreserveMethod(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(url));
            }

            return new RedirectResult(url: url, permanent: false, preserveMethod: true);
        }

        /// <summary>
        /// Creates a <see cref="RedirectResult"/> object with <see cref="RedirectResult.Permanent"/> set to true
        /// and <see cref="RedirectResult.PreserveMethod"/> set to true (<see cref="StatusCodes.Status308PermanentRedirect"/>) 
        /// using the specified <paramref name="url"/>.
        /// </summary>
        /// <param name="url">The URL to redirect to.</param>
        /// <returns>The created <see cref="RedirectResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectResult RedirectPermanentPreserveMethod(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(url));
            }

            return new RedirectResult(url: url, permanent: true, preserveMethod: true);
        }

        /// <summary>
        /// Creates a <see cref="LocalRedirectResult"/> object that redirects 
        /// (<see cref="StatusCodes.Status302Found"/>) to the specified local <paramref name="localUrl"/>.
        /// </summary>
        /// <param name="localUrl">The local URL to redirect to.</param>
        /// <returns>The created <see cref="LocalRedirectResult"/> for the response.</returns>
        [NonAction]
        public virtual LocalRedirectResult LocalRedirect(string localUrl)
        {
            if (string.IsNullOrEmpty(localUrl))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(localUrl));
            }

            return new LocalRedirectResult(localUrl);
        }

        /// <summary>
        /// Creates a <see cref="LocalRedirectResult"/> object with <see cref="LocalRedirectResult.Permanent"/> set to
        /// true (<see cref="StatusCodes.Status301MovedPermanently"/>) using the specified <paramref name="localUrl"/>.
        /// </summary>
        /// <param name="localUrl">The local URL to redirect to.</param>
        /// <returns>The created <see cref="LocalRedirectResult"/> for the response.</returns>
        [NonAction]
        public virtual LocalRedirectResult LocalRedirectPermanent(string localUrl)
        {
            if (string.IsNullOrEmpty(localUrl))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(localUrl));
            }

            return new LocalRedirectResult(localUrl, permanent: true);
        }

        /// <summary>
        /// Creates a <see cref="LocalRedirectResult"/> object with <see cref="LocalRedirectResult.Permanent"/> set to
        /// false and <see cref="LocalRedirectResult.PreserveMethod"/> set to true 
        /// (<see cref="StatusCodes.Status307TemporaryRedirect"/>) using the specified <paramref name="localUrl"/>.
        /// </summary>
        /// <param name="localUrl">The local URL to redirect to.</param>
        /// <returns>The created <see cref="LocalRedirectResult"/> for the response.</returns>
        [NonAction]
        public virtual LocalRedirectResult LocalRedirectPreserveMethod(string localUrl)
        {
            if (string.IsNullOrEmpty(localUrl))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(localUrl));
            }

            return new LocalRedirectResult(localUrl: localUrl, permanent: false, preserveMethod: true);
        }

        /// <summary>
        /// Creates a <see cref="LocalRedirectResult"/> object with <see cref="LocalRedirectResult.Permanent"/> set to
        /// true and <see cref="LocalRedirectResult.PreserveMethod"/> set to true 
        /// (<see cref="StatusCodes.Status308PermanentRedirect"/>) using the specified <paramref name="localUrl"/>.
        /// </summary>
        /// <param name="localUrl">The local URL to redirect to.</param>
        /// <returns>The created <see cref="LocalRedirectResult"/> for the response.</returns>
        [NonAction]
        public virtual LocalRedirectResult LocalRedirectPermanentPreserveMethod(string localUrl)
        {
            if (string.IsNullOrEmpty(localUrl))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(localUrl));
            }

            return new LocalRedirectResult(localUrl: localUrl, permanent: true, preserveMethod: true);
        }

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to an action with the same name as current one.
        /// The 'controller' and 'action' names are retrieved from the ambient values of the current request.
        /// </summary>
        /// <returns>The created <see cref="RedirectToActionResult"/> for the response.</returns>
        /// <example>
        /// A POST request to an action named "Product" updates a product and redirects to an action, also named
        /// "Product", showing details of the updated product.
        /// <code>
        /// [HttpGet]
        /// public IActionResult Product(int id)
        /// {
        ///     var product = RetrieveProduct(id);
        ///     return View(product);
        /// }
        ///
        /// [HttpPost]
        /// public IActionResult Product(int id, Product product)
        /// {
        ///     UpdateProduct(product);
        ///     return RedirectToAction();
        /// }
        /// </code>
        /// </example>
        [NonAction]
        public virtual RedirectToActionResult RedirectToAction()
            => RedirectToAction(actionName: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified action using the <paramref name="actionName"/>.
        /// </summary>
        /// <param name="actionName">The name of the action.</param>
        /// <returns>The created <see cref="RedirectToActionResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToActionResult RedirectToAction(string actionName)
            => RedirectToAction(actionName, routeValues: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified action using the 
        /// <paramref name="actionName"/> and <paramref name="routeValues"/>.
        /// </summary>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <returns>The created <see cref="RedirectToActionResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToActionResult RedirectToAction(string actionName, object routeValues)
            => RedirectToAction(actionName, controllerName: null, routeValues: routeValues);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified action using the 
        /// <paramref name="actionName"/> and the <paramref name="controllerName"/>.
        /// </summary>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <returns>The created <see cref="RedirectToActionResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToActionResult RedirectToAction(string actionName, string controllerName)
            => RedirectToAction(actionName, controllerName, routeValues: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified action using the specified
        /// <paramref name="actionName"/>, <paramref name="controllerName"/>, and <paramref name="routeValues"/>.
        /// </summary>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <returns>The created <see cref="RedirectToActionResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToActionResult RedirectToAction(
            string actionName,
            string controllerName,
            object routeValues)
            => RedirectToAction(actionName, controllerName, routeValues, fragment: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified action using the specified
        /// <paramref name="actionName"/>, <paramref name="controllerName"/>, and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The created <see cref="RedirectToActionResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToActionResult RedirectToAction(
            string actionName,
            string controllerName,
            string fragment)
            => RedirectToAction(actionName, controllerName, routeValues: null, fragment: fragment);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified action using the specified <paramref name="actionName"/>,
        /// <paramref name="controllerName"/>, <paramref name="routeValues"/>, and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The created <see cref="RedirectToActionResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToActionResult RedirectToAction(
            string actionName,
            string controllerName,
            object routeValues,
            string fragment)
        {
            return new RedirectToActionResult(actionName, controllerName, routeValues, fragment)
            {
                UrlHelper = Url,
            };
        }

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status307TemporaryRedirect"/>) to the specified action with 
        /// <see cref="RedirectToActionResult.Permanent"/> set to false and <see cref="RedirectToActionResult.PreserveMethod"/> 
        /// set to true, using the specified <paramref name="actionName"/>, <paramref name="controllerName"/>, 
        /// <paramref name="routeValues"/>, and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>       
        /// <returns>The created <see cref="RedirectToActionResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToActionResult RedirectToActionPreserveMethod(
            string actionName = null,
            string controllerName = null,
            object routeValues = null,
            string fragment = null)
        {
            return new RedirectToActionResult(
                actionName: actionName,
                controllerName: controllerName,
                routeValues: routeValues,
                permanent: false,
                preserveMethod: true,
                fragment: fragment)
            {
                UrlHelper = Url,
            };
        }

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified action with 
        /// <see cref="RedirectToActionResult.Permanent"/> set to true using the specified <paramref name="actionName"/>.
        /// </summary>
        /// <param name="actionName">The name of the action.</param>
        /// <returns>The created <see cref="RedirectToActionResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToActionResult RedirectToActionPermanent(string actionName)
            => RedirectToActionPermanent(actionName, routeValues: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified action with 
        /// <see cref="RedirectToActionResult.Permanent"/> set to true using the specified <paramref name="actionName"/> 
        /// and <paramref name="routeValues"/>.
        /// </summary>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <returns>The created <see cref="RedirectToActionResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToActionResult RedirectToActionPermanent(string actionName, object routeValues)
            => RedirectToActionPermanent(actionName, controllerName: null, routeValues: routeValues);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified action with 
        /// <see cref="RedirectToActionResult.Permanent"/> set to true using the specified <paramref name="actionName"/> 
        /// and <paramref name="controllerName"/>.
        /// </summary>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <returns>The created <see cref="RedirectToActionResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToActionResult RedirectToActionPermanent(string actionName, string controllerName)
            => RedirectToActionPermanent(actionName, controllerName, routeValues: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified action with 
        /// <see cref="RedirectToActionResult.Permanent"/> set to true using the specified <paramref name="actionName"/>,
        /// <paramref name="controllerName"/>, and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The created <see cref="RedirectToActionResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToActionResult RedirectToActionPermanent(
            string actionName,
            string controllerName,
            string fragment)
            => RedirectToActionPermanent(actionName, controllerName, routeValues: null, fragment: fragment);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified action with 
        /// <see cref="RedirectToActionResult.Permanent"/> set to true using the specified <paramref name="actionName"/>,
        /// <paramref name="controllerName"/>, and <paramref name="routeValues"/>.
        /// </summary>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <returns>The created <see cref="RedirectToActionResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToActionResult RedirectToActionPermanent(
            string actionName,
            string controllerName,
            object routeValues)
            => RedirectToActionPermanent(actionName, controllerName, routeValues, fragment: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified action with 
        /// <see cref="RedirectToActionResult.Permanent"/> set to true using the specified <paramref name="actionName"/>,
        /// <paramref name="controllerName"/>, <paramref name="routeValues"/>, and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The created <see cref="RedirectToActionResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToActionResult RedirectToActionPermanent(
            string actionName,
            string controllerName,
            object routeValues,
            string fragment)
        {
            return new RedirectToActionResult(
                actionName,
                controllerName,
                routeValues,
                permanent: true,
                fragment: fragment)
            {
                UrlHelper = Url,
            };
        }

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status308PermanentRedirect"/>) to the specified action with 
        /// <see cref="RedirectToActionResult.Permanent"/> set to true and <see cref="RedirectToActionResult.PreserveMethod"/>
        /// set to true, using the specified <paramref name="actionName"/>, <paramref name="controllerName"/>, 
        /// <paramref name="routeValues"/>, and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The created <see cref="RedirectToActionResult"/> for the response.</returns>        
        [NonAction]
        public virtual RedirectToActionResult RedirectToActionPermanentPreserveMethod(
            string actionName = null,
            string controllerName = null,
            object routeValues = null,
            string fragment = null)
        {
            return new RedirectToActionResult(
                actionName: actionName,
                controllerName: controllerName,
                routeValues: routeValues,
                permanent: true,
                preserveMethod: true,
                fragment: fragment)
            {
                UrlHelper = Url,
            };
        }

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified route using the specified <paramref name="routeName"/>.
        /// </summary>
        /// <param name="routeName">The name of the route.</param>
        /// <returns>The created <see cref="RedirectToRouteResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToRouteResult RedirectToRoute(string routeName)
            => RedirectToRoute(routeName, routeValues: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified route using the specified <paramref name="routeValues"/>.
        /// </summary>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <returns>The created <see cref="RedirectToRouteResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToRouteResult RedirectToRoute(object routeValues)
            => RedirectToRoute(routeName: null, routeValues: routeValues);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified route using the specified
        /// <paramref name="routeName"/> and <paramref name="routeValues"/>.
        /// </summary>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <returns>The created <see cref="RedirectToRouteResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToRouteResult RedirectToRoute(string routeName, object routeValues)
            => RedirectToRoute(routeName, routeValues, fragment: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified route using the specified
        /// <paramref name="routeName"/> and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The created <see cref="RedirectToRouteResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToRouteResult RedirectToRoute(string routeName, string fragment)
            => RedirectToRoute(routeName, routeValues: null, fragment: fragment);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified route using the specified
        /// <paramref name="routeName"/>, <paramref name="routeValues"/>, and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The created <see cref="RedirectToRouteResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToRouteResult RedirectToRoute(
            string routeName,
            object routeValues,
            string fragment)
        {
            return new RedirectToRouteResult(routeName, routeValues, fragment)
            {
                UrlHelper = Url,
            };
        }

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status307TemporaryRedirect"/>) to the specified route with 
        /// <see cref="RedirectToRouteResult.Permanent"/> set to false and <see cref="RedirectToRouteResult.PreserveMethod"/>
        /// set to true, using the specified <paramref name="routeName"/>, <paramref name="routeValues"/>, and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The created <see cref="RedirectToRouteResult"/> for the response.</returns>       
        [NonAction]
        public virtual RedirectToRouteResult RedirectToRoutePreserveMethod(
            string routeName = null,
            object routeValues = null,
            string fragment = null)
        {
            return new RedirectToRouteResult(
                routeName: routeName,
                routeValues: routeValues,
                permanent: false,
                preserveMethod: true,
                fragment: fragment)
            {
                UrlHelper = Url,
            };
        }

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified route with 
        /// <see cref="RedirectToRouteResult.Permanent"/> set to true using the specified <paramref name="routeName"/>.
        /// </summary>
        /// <param name="routeName">The name of the route.</param>
        /// <returns>The created <see cref="RedirectToRouteResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToRouteResult RedirectToRoutePermanent(string routeName)
            => RedirectToRoutePermanent(routeName, routeValues: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified route with 
        /// <see cref="RedirectToRouteResult.Permanent"/> set to true using the specified <paramref name="routeValues"/>.
        /// </summary>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <returns>The created <see cref="RedirectToRouteResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToRouteResult RedirectToRoutePermanent(object routeValues)
            => RedirectToRoutePermanent(routeName: null, routeValues: routeValues);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified route with
        /// <see cref="RedirectToRouteResult.Permanent"/> set to true using the specified <paramref name="routeName"/>
        /// and <paramref name="routeValues"/>.
        /// </summary>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <returns>The created <see cref="RedirectToRouteResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToRouteResult RedirectToRoutePermanent(string routeName, object routeValues)
            => RedirectToRoutePermanent(routeName, routeValues, fragment: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified route with 
        /// <see cref="RedirectToRouteResult.Permanent"/> set to true using the specified <paramref name="routeName"/>
        /// and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The created <see cref="RedirectToRouteResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToRouteResult RedirectToRoutePermanent(string routeName, string fragment)
            => RedirectToRoutePermanent(routeName, routeValues: null, fragment: fragment);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified route with
        /// <see cref="RedirectToRouteResult.Permanent"/> set to true using the specified <paramref name="routeName"/>,
        /// <paramref name="routeValues"/>, and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The created <see cref="RedirectToRouteResult"/> for the response.</returns>
        [NonAction]
        public virtual RedirectToRouteResult RedirectToRoutePermanent(
            string routeName,
            object routeValues,
            string fragment)
        {
            return new RedirectToRouteResult(routeName, routeValues, permanent: true, fragment: fragment)
            {
                UrlHelper = Url,
            };
        }

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status308PermanentRedirect"/>) to the specified route with
        /// <see cref="RedirectToRouteResult.Permanent"/> set to true and <see cref="RedirectToRouteResult.PreserveMethod"/>
        /// set to true, using the specified <paramref name="routeName"/>, <paramref name="routeValues"/>, and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The created <see cref="RedirectToRouteResult"/> for the response.</returns>       
        [NonAction]
        public virtual RedirectToRouteResult RedirectToRoutePermanentPreserveMethod(
            string routeName = null,
            object routeValues = null,
            string fragment = null)
        {
            return new RedirectToRouteResult(
                routeName: routeName,
                routeValues: routeValues,
                permanent: true,
                preserveMethod: true,
                fragment: fragment)
            {
                UrlHelper = Url,
            };
        }

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified <paramref name="pageName"/>.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <returns>The <see cref="RedirectToPageResult"/>.</returns>
        [NonAction]
        public virtual RedirectToPageResult RedirectToPage(string pageName)
            => RedirectToPage(pageName, routeValues: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified <paramref name="pageName"/>
        /// using the specified <paramref name="routeValues"/>.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <returns>The <see cref="RedirectToPageResult"/>.</returns>
        [NonAction]
        public virtual RedirectToPageResult RedirectToPage(string pageName, object routeValues)
            => RedirectToPage(pageName, pageHandler: null, routeValues: routeValues, fragment: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified <paramref name="pageName"/>
        /// using the specified <paramref name="pageHandler"/>.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <param name="pageHandler">The page handler to redirect to.</param>
        /// <returns>The <see cref="RedirectToPageResult"/>.</returns>
        [NonAction]
        public virtual RedirectToPageResult RedirectToPage(string pageName, string pageHandler)
            => RedirectToPage(pageName, pageHandler, routeValues: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified <paramref name="pageName"/>.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <param name="pageHandler">The page handler to redirect to.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <returns>The <see cref="RedirectToPageResult"/>.</returns>
        [NonAction]
        public virtual RedirectToPageResult RedirectToPage(string pageName, string pageHandler, object routeValues)
            => RedirectToPage(pageName, pageHandler, routeValues, fragment: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified <paramref name="pageName"/>
        /// using the specified <paramref name="fragment"/>.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <param name="pageHandler">The page handler to redirect to.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The <see cref="RedirectToPageResult"/>.</returns>
        [NonAction]
        public virtual RedirectToPageResult RedirectToPage(string pageName, string pageHandler, string fragment)
            => RedirectToPage(pageName, pageHandler, routeValues: null, fragment: fragment);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified <paramref name="pageName"/>
        /// using the specified <paramref name="routeValues"/> and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <param name="pageHandler">The page handler to redirect to.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The <see cref="RedirectToPageResult"/>.</returns>
        [NonAction]
        public virtual RedirectToPageResult RedirectToPage(string pageName, string pageHandler, object routeValues, string fragment)
            => new RedirectToPageResult(pageName, pageHandler, routeValues, fragment);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified <paramref name="pageName"/>.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <returns>The <see cref="RedirectToPageResult"/> with <see cref="RedirectToPageResult.Permanent"/> set.</returns>
        [NonAction]
        public virtual RedirectToPageResult RedirectToPagePermanent(string pageName)
            => RedirectToPagePermanent(pageName, routeValues: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified <paramref name="pageName"/>
        /// using the specified <paramref name="routeValues"/>.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <returns>The <see cref="RedirectToPageResult"/> with <see cref="RedirectToPageResult.Permanent"/> set.</returns>
        [NonAction]
        public virtual RedirectToPageResult RedirectToPagePermanent(string pageName, object routeValues)
            => RedirectToPagePermanent(pageName, pageHandler: null, routeValues: routeValues, fragment: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified <paramref name="pageName"/>
        /// using the specified <paramref name="pageHandler"/>.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <param name="pageHandler">The page handler to redirect to.</param>
        /// <returns>The <see cref="RedirectToPageResult"/> with <see cref="RedirectToPageResult.Permanent"/> set.</returns>
        [NonAction]
        public virtual RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler)
            => RedirectToPagePermanent(pageName, pageHandler, routeValues: null, fragment: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified <paramref name="pageName"/>
        /// using the specified <paramref name="fragment"/>.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <param name="pageHandler">The page handler to redirect to.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The <see cref="RedirectToPageResult"/> with <see cref="RedirectToPageResult.Permanent"/> set.</returns>
        [NonAction]
        public virtual RedirectToPageResult RedirectToPagePermanent(string pageName, string pageHandler, string fragment)
            => RedirectToPagePermanent(pageName, pageHandler, routeValues: null, fragment: fragment);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified <paramref name="pageName"/>
        /// using the specified <paramref name="routeValues"/> and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <param name="pageHandler">The page handler to redirect to.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The <see cref="RedirectToPageResult"/> with <see cref="RedirectToPageResult.Permanent"/> set.</returns>
        [NonAction]
        public virtual RedirectToPageResult RedirectToPagePermanent(
            string pageName,
            string pageHandler,
            object routeValues,
            string fragment)
            => new RedirectToPageResult(pageName, pageHandler, routeValues, permanent: true, fragment: fragment);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status307TemporaryRedirect"/>) to the specified page with 
        /// <see cref="RedirectToRouteResult.Permanent"/> set to false and <see cref="RedirectToRouteResult.PreserveMethod"/>
        /// set to true, using the specified <paramref name="pageName"/>, <paramref name="routeValues"/>, and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <param name="pageHandler">The page handler to redirect to.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The created <see cref="RedirectToRouteResult"/> for the response.</returns> 
        [NonAction]
        public virtual RedirectToPageResult RedirectToPagePreserveMethod(
            string pageName,
            string pageHandler = null,
            object routeValues = null,
            string fragment = null)
        {
            if (pageName == null)
            {
                throw new ArgumentNullException(nameof(pageName));
            }

            return new RedirectToPageResult(
                pageName: pageName,
                pageHandler: pageHandler,
                routeValues: routeValues,
                permanent: false,
                preserveMethod: true,
                fragment: fragment);
        }

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status308PermanentRedirect"/>) to the specified route with
        /// <see cref="RedirectToRouteResult.Permanent"/> set to true and <see cref="RedirectToRouteResult.PreserveMethod"/>
        /// set to true, using the specified <paramref name="pageName"/>, <paramref name="routeValues"/>, and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <param name="pageHandler">The page handler to redirect to.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The created <see cref="RedirectToRouteResult"/> for the response.</returns>  
        [NonAction]
        public virtual RedirectToPageResult RedirectToPagePermanentPreserveMethod(
            string pageName,
            string pageHandler = null,
            object routeValues = null,
            string fragment = null)
        {
            if (pageName == null)
            {
                throw new ArgumentNullException(nameof(pageName));
            }

            return new RedirectToPageResult(
                pageName: pageName,
                pageHandler: pageHandler,
                routeValues: routeValues,
                permanent: true,
                preserveMethod: true,
                fragment: fragment);
        }
        #endregion

        #region FileResult variants
        /// <summary>
        /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>),
        /// and the specified <paramref name="contentType" /> as the Content-Type.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="fileContents">The file contents.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <returns>The created <see cref="FileContentResult"/> for the response.</returns>
        [NonAction]
        public virtual FileContentResult File(byte[] fileContents, string contentType)
            => File(fileContents, contentType, fileDownloadName: null);

        /// <summary>
        /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>),
        /// and the specified <paramref name="contentType" /> as the Content-Type.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="fileContents">The file contents.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="FileContentResult"/> for the response.</returns>
        [NonAction]
        public virtual FileContentResult File(byte[] fileContents, string contentType, bool enableRangeProcessing)
            => File(fileContents, contentType, fileDownloadName: null, enableRangeProcessing: enableRangeProcessing);

        /// <summary>
        /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>), the
        /// specified <paramref name="contentType" /> as the Content-Type and the specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="fileContents">The file contents.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <returns>The created <see cref="FileContentResult"/> for the response.</returns>
        [NonAction]
        public virtual FileContentResult File(byte[] fileContents, string contentType, string fileDownloadName)
            => new FileContentResult(fileContents, contentType) { FileDownloadName = fileDownloadName };

        /// <summary>
        /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>), the
        /// specified <paramref name="contentType" /> as the Content-Type and the specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="fileContents">The file contents.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="FileContentResult"/> for the response.</returns>
        [NonAction]
        public virtual FileContentResult File(byte[] fileContents, string contentType, string fileDownloadName, bool enableRangeProcessing)
            => new FileContentResult(fileContents, contentType)
            {
                FileDownloadName = fileDownloadName,
                EnableRangeProcessing = enableRangeProcessing,
            };

        /// <summary>
        /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>), 
        /// and the specified <paramref name="contentType" /> as the Content-Type.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="fileContents">The file contents.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <returns>The created <see cref="FileContentResult"/> for the response.</returns>
        [NonAction]
        public virtual FileContentResult File(byte[] fileContents, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
        {
            return new FileContentResult(fileContents, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
            };
        }

        /// <summary>
        /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>), 
        /// and the specified <paramref name="contentType" /> as the Content-Type.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="fileContents">The file contents.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="FileContentResult"/> for the response.</returns>
        [NonAction]
        public virtual FileContentResult File(byte[] fileContents, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
        {
            return new FileContentResult(fileContents, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                EnableRangeProcessing = enableRangeProcessing,
            };
        }

        /// <summary>
        /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>), the 
        /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="fileContents">The file contents.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <returns>The created <see cref="FileContentResult"/> for the response.</returns>
        [NonAction]
        public virtual FileContentResult File(byte[] fileContents, string contentType, string fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
        {
            return new FileContentResult(fileContents, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                FileDownloadName = fileDownloadName,
            };
        }

        /// <summary>
        /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>), the 
        /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="fileContents">The file contents.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="FileContentResult"/> for the response.</returns>
        [NonAction]
        public virtual FileContentResult File(byte[] fileContents, string contentType, string fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
        {
            return new FileContentResult(fileContents, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                FileDownloadName = fileDownloadName,
                EnableRangeProcessing = enableRangeProcessing,
            };
        }

        /// <summary>
        /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>), with the 
        /// specified <paramref name="contentType" /> as the Content-Type.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <returns>The created <see cref="FileStreamResult"/> for the response.</returns>
        /// <remarks>
        /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
        /// </remarks>
        [NonAction]
        public virtual FileStreamResult File(Stream fileStream, string contentType)
            => File(fileStream, contentType, fileDownloadName: null);

        /// <summary>
        /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>), with the 
        /// specified <paramref name="contentType" /> as the Content-Type.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="FileStreamResult"/> for the response.</returns>
        /// <remarks>
        /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
        /// </remarks>        
        [NonAction]
        public virtual FileStreamResult File(Stream fileStream, string contentType, bool enableRangeProcessing)
            => File(fileStream, contentType, fileDownloadName: null, enableRangeProcessing: enableRangeProcessing);

        /// <summary>
        /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>) with the
        /// specified <paramref name="contentType" /> as the Content-Type and the
        /// specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <returns>The created <see cref="FileStreamResult"/> for the response.</returns>
        /// <remarks>
        /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
        /// </remarks>        
        [NonAction]
        public virtual FileStreamResult File(Stream fileStream, string contentType, string fileDownloadName)
            => new FileStreamResult(fileStream, contentType) { FileDownloadName = fileDownloadName };

        /// <summary>
        /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>) with the
        /// specified <paramref name="contentType" /> as the Content-Type and the
        /// specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="FileStreamResult"/> for the response.</returns>
        /// <remarks>
        /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
        /// </remarks>        
        [NonAction]
        public virtual FileStreamResult File(Stream fileStream, string contentType, string fileDownloadName, bool enableRangeProcessing)
            => new FileStreamResult(fileStream, contentType)
            {
                FileDownloadName = fileDownloadName,
                EnableRangeProcessing = enableRangeProcessing,
            };

        /// <summary>
        /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>), 
        /// and the specified <paramref name="contentType" /> as the Content-Type.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <returns>The created <see cref="FileStreamResult"/> for the response.</returns>
        /// <remarks>
        /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
        /// </remarks>        
        [NonAction]
        public virtual FileStreamResult File(Stream fileStream, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
        {
            return new FileStreamResult(fileStream, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
            };
        }

        /// <summary>
        /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>), 
        /// and the specified <paramref name="contentType" /> as the Content-Type.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="FileStreamResult"/> for the response.</returns>
        /// <remarks>
        /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
        /// </remarks>        
        [NonAction]
        public virtual FileStreamResult File(Stream fileStream, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
        {
            return new FileStreamResult(fileStream, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                EnableRangeProcessing = enableRangeProcessing,
            };
        }

        /// <summary>
        /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>), the 
        /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <returns>The created <see cref="FileStreamResult"/> for the response.</returns>
        /// <remarks>
        /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
        /// </remarks>        
        [NonAction]
        public virtual FileStreamResult File(Stream fileStream, string contentType, string fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
        {
            return new FileStreamResult(fileStream, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                FileDownloadName = fileDownloadName,
            };
        }

        /// <summary>
        /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>), the 
        /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="FileStreamResult"/> for the response.</returns>
        /// <remarks>
        /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
        /// </remarks>        
        [NonAction]
        public virtual FileStreamResult File(Stream fileStream, string contentType, string fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
        {
            return new FileStreamResult(fileStream, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                FileDownloadName = fileDownloadName,
                EnableRangeProcessing = enableRangeProcessing,
            };
        }

        /// <summary>
        /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
        /// specified <paramref name="contentType" /> as the Content-Type.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="virtualPath">The virtual path of the file to be returned.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <returns>The created <see cref="VirtualFileResult"/> for the response.</returns>
        [NonAction]
        public virtual VirtualFileResult File(string virtualPath, string contentType)
            => File(virtualPath, contentType, fileDownloadName: null);

        /// <summary>
        /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
        /// specified <paramref name="contentType" /> as the Content-Type.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="virtualPath">The virtual path of the file to be returned.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="VirtualFileResult"/> for the response.</returns>
        [NonAction]
        public virtual VirtualFileResult File(string virtualPath, string contentType, bool enableRangeProcessing)
            => File(virtualPath, contentType, fileDownloadName: null, enableRangeProcessing: enableRangeProcessing);

        /// <summary>
        /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
        /// specified <paramref name="contentType" /> as the Content-Type and the
        /// specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="virtualPath">The virtual path of the file to be returned.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <returns>The created <see cref="VirtualFileResult"/> for the response.</returns>
        [NonAction]
        public virtual VirtualFileResult File(string virtualPath, string contentType, string fileDownloadName)
            => new VirtualFileResult(virtualPath, contentType) { FileDownloadName = fileDownloadName };

        /// <summary>
        /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
        /// specified <paramref name="contentType" /> as the Content-Type and the
        /// specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="virtualPath">The virtual path of the file to be returned.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="VirtualFileResult"/> for the response.</returns>
        [NonAction]
        public virtual VirtualFileResult File(string virtualPath, string contentType, string fileDownloadName, bool enableRangeProcessing)
            => new VirtualFileResult(virtualPath, contentType)
            {
                FileDownloadName = fileDownloadName,
                EnableRangeProcessing = enableRangeProcessing,
            };

        /// <summary>
        /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>), and the 
        /// specified <paramref name="contentType" /> as the Content-Type.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="virtualPath">The virtual path of the file to be returned.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <returns>The created <see cref="VirtualFileResult"/> for the response.</returns>
        [NonAction]
        public virtual VirtualFileResult File(string virtualPath, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
        {
            return new VirtualFileResult(virtualPath, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
            };
        }

        /// <summary>
        /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>), and the 
        /// specified <paramref name="contentType" /> as the Content-Type.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="virtualPath">The virtual path of the file to be returned.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="VirtualFileResult"/> for the response.</returns>
        [NonAction]
        public virtual VirtualFileResult File(string virtualPath, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
        {
            return new VirtualFileResult(virtualPath, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                EnableRangeProcessing = enableRangeProcessing,
            };
        }

        /// <summary>
        /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>), the 
        /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="virtualPath">The virtual path of the file to be returned.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <returns>The created <see cref="VirtualFileResult"/> for the response.</returns>
        [NonAction]
        public virtual VirtualFileResult File(string virtualPath, string contentType, string fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
        {
            return new VirtualFileResult(virtualPath, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                FileDownloadName = fileDownloadName,
            };
        }

        /// <summary>
        /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>), the 
        /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="virtualPath">The virtual path of the file to be returned.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="VirtualFileResult"/> for the response.</returns>
        [NonAction]
        public virtual VirtualFileResult File(string virtualPath, string contentType, string fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
        {
            return new VirtualFileResult(virtualPath, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                FileDownloadName = fileDownloadName,
                EnableRangeProcessing = enableRangeProcessing,
            };
        }

        /// <summary>
        /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
        /// specified <paramref name="contentType" /> as the Content-Type.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <returns>The created <see cref="PhysicalFileResult"/> for the response.</returns>
        [NonAction]
        public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType)
            => PhysicalFile(physicalPath, contentType, fileDownloadName: null);

        /// <summary>
        /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
        /// specified <paramref name="contentType" /> as the Content-Type.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="PhysicalFileResult"/> for the response.</returns>
        [NonAction]
        public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType, bool enableRangeProcessing)
            => PhysicalFile(physicalPath, contentType, fileDownloadName: null, enableRangeProcessing: enableRangeProcessing);

        /// <summary>
        /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
        /// specified <paramref name="contentType" /> as the Content-Type and the
        /// specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <returns>The created <see cref="PhysicalFileResult"/> for the response.</returns>
        [NonAction]
        public virtual PhysicalFileResult PhysicalFile(
            string physicalPath,
            string contentType,
            string fileDownloadName)
            => new PhysicalFileResult(physicalPath, contentType) { FileDownloadName = fileDownloadName };

        /// <summary>
        /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
        /// specified <paramref name="contentType" /> as the Content-Type and the
        /// specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="PhysicalFileResult"/> for the response.</returns>
        [NonAction]
        public virtual PhysicalFileResult PhysicalFile(
            string physicalPath,
            string contentType,
            string fileDownloadName,
            bool enableRangeProcessing)
            => new PhysicalFileResult(physicalPath, contentType)
            {
                FileDownloadName = fileDownloadName,
                EnableRangeProcessing = enableRangeProcessing,
            };

        /// <summary>
        /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>), and 
        /// the specified <paramref name="contentType" /> as the Content-Type.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <returns>The created <see cref="PhysicalFileResult"/> for the response.</returns>
        [NonAction]
        public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
        {
            return new PhysicalFileResult(physicalPath, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
            };
        }

        /// <summary>
        /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>), and 
        /// the specified <paramref name="contentType" /> as the Content-Type.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="PhysicalFileResult"/> for the response.</returns>
        [NonAction]
        public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
        {
            return new PhysicalFileResult(physicalPath, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                EnableRangeProcessing = enableRangeProcessing,
            };
        }

        /// <summary>
        /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>), the 
        /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <returns>The created <see cref="PhysicalFileResult"/> for the response.</returns>
        [NonAction]
        public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType, string fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
        {
            return new PhysicalFileResult(physicalPath, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                FileDownloadName = fileDownloadName,
            };
        }

        /// <summary>
        /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>), the 
        /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </summary>
        /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="PhysicalFileResult"/> for the response.</returns>
        [NonAction]
        public virtual PhysicalFileResult PhysicalFile(string physicalPath, string contentType, string fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
        {
            return new PhysicalFileResult(physicalPath, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                FileDownloadName = fileDownloadName,
                EnableRangeProcessing = enableRangeProcessing,
            };
        }
        #endregion

        /// <summary>
        /// Creates an <see cref="UnauthorizedResult"/> that produces an <see cref="StatusCodes.Status401Unauthorized"/> response.
        /// </summary>
        /// <returns>The created <see cref="UnauthorizedResult"/> for the response.</returns>
        [NonAction]
        public virtual UnauthorizedResult Unauthorized()
            => new UnauthorizedResult();

        /// <summary>
        /// Creates an <see cref="UnauthorizedObjectResult"/> that produces a <see cref="StatusCodes.Status401Unauthorized"/> response.
        /// </summary>
        /// <returns>The created <see cref="UnauthorizedObjectResult"/> for the response.</returns>
        [NonAction]
        public virtual UnauthorizedObjectResult Unauthorized([ActionResultObjectValue] object value)
            => new UnauthorizedObjectResult(value);

        /// <summary>
        /// Creates an <see cref="NotFoundResult"/> that produces a <see cref="StatusCodes.Status404NotFound"/> response.
        /// </summary>
        /// <returns>The created <see cref="NotFoundResult"/> for the response.</returns>
        [NonAction]
        public virtual NotFoundResult NotFound()
            => new NotFoundResult();

        /// <summary>
        /// Creates an <see cref="NotFoundObjectResult"/> that produces a <see cref="StatusCodes.Status404NotFound"/> response.
        /// </summary>
        /// <returns>The created <see cref="NotFoundObjectResult"/> for the response.</returns>
        [NonAction]
        public virtual NotFoundObjectResult NotFound([ActionResultObjectValue] object value)
            => new NotFoundObjectResult(value);

        /// <summary>
        /// Creates an <see cref="BadRequestResult"/> that produces a <see cref="StatusCodes.Status400BadRequest"/> response.
        /// </summary>
        /// <returns>The created <see cref="BadRequestResult"/> for the response.</returns>
        [NonAction]
        public virtual BadRequestResult BadRequest()
            => new BadRequestResult();

        /// <summary>
        /// Creates an <see cref="BadRequestObjectResult"/> that produces a <see cref="StatusCodes.Status400BadRequest"/> response.
        /// </summary>
        /// <param name="error">An error object to be returned to the client.</param>
        /// <returns>The created <see cref="BadRequestObjectResult"/> for the response.</returns>
        [NonAction]
        public virtual BadRequestObjectResult BadRequest([ActionResultObjectValue] object error)
            => new BadRequestObjectResult(error);

        /// <summary>
        /// Creates an <see cref="BadRequestObjectResult"/> that produces a <see cref="StatusCodes.Status400BadRequest"/> response.
        /// </summary>
        /// <param name="modelState">The <see cref="ModelStateDictionary" /> containing errors to be returned to the client.</param>
        /// <returns>The created <see cref="BadRequestObjectResult"/> for the response.</returns>
        [NonAction]
        public virtual BadRequestObjectResult BadRequest([ActionResultObjectValue] ModelStateDictionary modelState)
        {
            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            return new BadRequestObjectResult(modelState);
        }

        /// <summary>
        /// Creates an <see cref="UnprocessableEntityResult"/> that produces a <see cref="StatusCodes.Status422UnprocessableEntity"/> response.
        /// </summary>
        /// <returns>The created <see cref="UnprocessableEntityResult"/> for the response.</returns>
        [NonAction]
        public virtual UnprocessableEntityResult UnprocessableEntity()
            => new UnprocessableEntityResult();

        /// <summary>
        /// Creates an <see cref="UnprocessableEntityObjectResult"/> that produces a <see cref="StatusCodes.Status422UnprocessableEntity"/> response.
        /// </summary>
        /// <param name="error">An error object to be returned to the client.</param>
        /// <returns>The created <see cref="UnprocessableEntityObjectResult"/> for the response.</returns>
        [NonAction]
        public virtual UnprocessableEntityObjectResult UnprocessableEntity([ActionResultObjectValue] object error)
            => new UnprocessableEntityObjectResult(error);

        /// <summary>
        /// Creates an <see cref="UnprocessableEntityObjectResult"/> that produces a <see cref="StatusCodes.Status422UnprocessableEntity"/> response.
        /// </summary>
        /// <param name="modelState">The <see cref="ModelStateDictionary" /> containing errors to be returned to the client.</param>
        /// <returns>The created <see cref="UnprocessableEntityObjectResult"/> for the response.</returns>
        [NonAction]
        public virtual UnprocessableEntityObjectResult UnprocessableEntity([ActionResultObjectValue] ModelStateDictionary modelState)
        {
            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            return new UnprocessableEntityObjectResult(modelState);
        }

        /// <summary>
        /// Creates an <see cref="ConflictResult"/> that produces a <see cref="StatusCodes.Status409Conflict"/> response.
        /// </summary>
        /// <returns>The created <see cref="ConflictResult"/> for the response.</returns>
        [NonAction]
        public virtual ConflictResult Conflict()
            => new ConflictResult();

        /// <summary>
        /// Creates an <see cref="ConflictObjectResult"/> that produces a <see cref="StatusCodes.Status409Conflict"/> response.
        /// </summary>
        /// <param name="error">Contains errors to be returned to the client.</param>
        /// <returns>The created <see cref="ConflictObjectResult"/> for the response.</returns>
        [NonAction]
        public virtual ConflictObjectResult Conflict([ActionResultObjectValue] object error)
            => new ConflictObjectResult(error);

        /// <summary>
        /// Creates an <see cref="ConflictObjectResult"/> that produces a <see cref="StatusCodes.Status409Conflict"/> response.
        /// </summary>
        /// <param name="modelState">The <see cref="ModelStateDictionary" /> containing errors to be returned to the client.</param>
        /// <returns>The created <see cref="ConflictObjectResult"/> for the response.</returns>
        [NonAction]
        public virtual ConflictObjectResult Conflict([ActionResultObjectValue] ModelStateDictionary modelState)
            => new ConflictObjectResult(modelState);

        /// <summary>
        /// Creates an <see cref="ObjectResult"/> that produces a <see cref="ProblemDetails"/> response.
        /// </summary>
        /// <param name="statusCode">The value for <see cref="ProblemDetails.Status" />.</param>
        /// <param name="detail">The value for <see cref="ProblemDetails.Detail" />.</param>
        /// <param name="instance">The value for <see cref="ProblemDetails.Instance" />.</param>
        /// <param name="title">The value for <see cref="ProblemDetails.Title" />.</param>
        /// <param name="type">The value for <see cref="ProblemDetails.Type" />.</param>
        /// <returns>The created <see cref="ObjectResult"/> for the response.</returns>
        [NonAction]
        public virtual ObjectResult Problem(
            string detail = null,
            string instance = null,
            int? statusCode = null,
            string title = null,
            string type = null)
        {
            var problemDetails = ProblemDetailsFactory.CreateProblemDetails(
                HttpContext,
                statusCode: statusCode ?? 500,
                title: title,
                type: type,
                detail: detail,
                instance: instance);

            return new ObjectResult(problemDetails)
            {
                StatusCode = problemDetails.Status
            };
        }

        /// <summary>
        /// Creates an <see cref="BadRequestObjectResult"/> that produces a <see cref="StatusCodes.Status400BadRequest"/> response.
        /// </summary>
        /// <returns>The created <see cref="BadRequestObjectResult"/> for the response.</returns>
        [NonAction]
        public virtual ActionResult ValidationProblem([ActionResultObjectValue] ValidationProblemDetails descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return new BadRequestObjectResult(descriptor);
        }

        /// <summary>
        /// Creates an <see cref="ActionResult"/> that produces a <see cref="StatusCodes.Status400BadRequest"/> response
        /// with validation errors from <paramref name="modelStateDictionary"/>.
        /// </summary>
        /// <param name="modelStateDictionary">The <see cref="ModelStateDictionary"/>.</param>
        /// <returns>The created <see cref="BadRequestObjectResult"/> for the response.</returns>
        [NonAction]
        public virtual ActionResult ValidationProblem([ActionResultObjectValue] ModelStateDictionary modelStateDictionary)
             => ValidationProblem(detail: null, modelStateDictionary: modelStateDictionary);


        /// <summary>
        /// Creates an <see cref="ActionResult"/> that produces a <see cref="StatusCodes.Status400BadRequest"/> response
        /// with validation errors from <see cref="ModelState"/>.
        /// </summary>
        /// <returns>The created <see cref="ActionResult"/> for the response.</returns>
        [NonAction]
        public virtual ActionResult ValidationProblem()
            => ValidationProblem(ModelState);

        /// <summary>
        /// Creates an <see cref="ActionResult"/> that produces a <see cref="StatusCodes.Status400BadRequest"/> response
        /// with a <see cref="ValidationProblemDetails"/> value.
        /// </summary>
        /// <param name="detail">The value for <see cref="ProblemDetails.Detail" />.</param>
        /// <param name="instance">The value for <see cref="ProblemDetails.Instance" />.</param>
        /// <param name="statusCode">The status code.</param>
        /// <param name="title">The value for <see cref="ProblemDetails.Title" />.</param>
        /// <param name="type">The value for <see cref="ProblemDetails.Type" />.</param>
        /// <param name="modelStateDictionary">The <see cref="ModelStateDictionary"/>. 
        /// When <see langword="null"/> uses <see cref="ModelState"/>.</param>
        /// <returns>The created <see cref="ActionResult"/> for the response.</returns>
        [NonAction]
        public virtual ActionResult ValidationProblem(
            string detail = null,
            string instance = null,
            int? statusCode = null,
            string title = null,
            string type = null,
            [ActionResultObjectValue] ModelStateDictionary modelStateDictionary = null)
        {
            modelStateDictionary ??= ModelState;

            var validationProblem = ProblemDetailsFactory.CreateValidationProblemDetails(
                HttpContext,
                modelStateDictionary,
                statusCode: statusCode,
                title: title,
                type: type,
                detail: detail,
                instance: instance);

            if (validationProblem.Status == 400)
            {
                // For compatibility with 2.x, continue producing BadRequestObjectResult instances if the status code is 400.
                return new BadRequestObjectResult(validationProblem);
            }

            return new ObjectResult(validationProblem)
            {
                StatusCode = validationProblem.Status
            };
        }

        /// <summary>
        /// Creates a <see cref="CreatedResult"/> object that produces a <see cref="StatusCodes.Status201Created"/> response.
        /// </summary>
        /// <param name="uri">The URI at which the content has been created.</param>
        /// <param name="value">The content value to format in the entity body.</param>
        /// <returns>The created <see cref="CreatedResult"/> for the response.</returns>
        [NonAction]
        public virtual CreatedResult Created(string uri, [ActionResultObjectValue] object value)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            return new CreatedResult(uri, value);
        }

        /// <summary>
        /// Creates a <see cref="CreatedResult"/> object that produces a <see cref="StatusCodes.Status201Created"/> response.
        /// </summary>
        /// <param name="uri">The URI at which the content has been created.</param>
        /// <param name="value">The content value to format in the entity body.</param>
        /// <returns>The created <see cref="CreatedResult"/> for the response.</returns>
        [NonAction]
        public virtual CreatedResult Created(Uri uri, [ActionResultObjectValue] object value)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            return new CreatedResult(uri, value);
        }

        /// <summary>
        /// Creates a <see cref="CreatedAtActionResult"/> object that produces a <see cref="StatusCodes.Status201Created"/> response.
        /// </summary>
        /// <param name="actionName">The name of the action to use for generating the URL.</param>
        /// <param name="value">The content value to format in the entity body.</param>
        /// <returns>The created <see cref="CreatedAtActionResult"/> for the response.</returns>
        [NonAction]
        public virtual CreatedAtActionResult CreatedAtAction(string actionName, [ActionResultObjectValue] object value)
            => CreatedAtAction(actionName, routeValues: null, value: value);

        /// <summary>
        /// Creates a <see cref="CreatedAtActionResult"/> object that produces a <see cref="StatusCodes.Status201Created"/> response.
        /// </summary>
        /// <param name="actionName">The name of the action to use for generating the URL.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="value">The content value to format in the entity body.</param>
        /// <returns>The created <see cref="CreatedAtActionResult"/> for the response.</returns>
        [NonAction]
        public virtual CreatedAtActionResult CreatedAtAction(string actionName, object routeValues, [ActionResultObjectValue] object value)
            => CreatedAtAction(actionName, controllerName: null, routeValues: routeValues, value: value);

        /// <summary>
        /// Creates a <see cref="CreatedAtActionResult"/> object that produces a <see cref="StatusCodes.Status201Created"/> response.
        /// </summary>
        /// <param name="actionName">The name of the action to use for generating the URL.</param>
        /// <param name="controllerName">The name of the controller to use for generating the URL.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="value">The content value to format in the entity body.</param>
        /// <returns>The created <see cref="CreatedAtActionResult"/> for the response.</returns>
        [NonAction]
        public virtual CreatedAtActionResult CreatedAtAction(
            string actionName,
            string controllerName,
            object routeValues,
            [ActionResultObjectValue] object value)
            => new CreatedAtActionResult(actionName, controllerName, routeValues, value);

        /// <summary>
        /// Creates a <see cref="CreatedAtRouteResult"/> object that produces a <see cref="StatusCodes.Status201Created"/> response.
        /// </summary>
        /// <param name="routeName">The name of the route to use for generating the URL.</param>
        /// <param name="value">The content value to format in the entity body.</param>
        /// <returns>The created <see cref="CreatedAtRouteResult"/> for the response.</returns>
        [NonAction]
        public virtual CreatedAtRouteResult CreatedAtRoute(string routeName, [ActionResultObjectValue] object value)
            => CreatedAtRoute(routeName, routeValues: null, value: value);

        /// <summary>
        /// Creates a <see cref="CreatedAtRouteResult"/> object that produces a <see cref="StatusCodes.Status201Created"/> response.
        /// </summary>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="value">The content value to format in the entity body.</param>
        /// <returns>The created <see cref="CreatedAtRouteResult"/> for the response.</returns>
        [NonAction]
        public virtual CreatedAtRouteResult CreatedAtRoute(object routeValues, [ActionResultObjectValue] object value)
            => CreatedAtRoute(routeName: null, routeValues: routeValues, value: value);

        /// <summary>
        /// Creates a <see cref="CreatedAtRouteResult"/> object that produces a <see cref="StatusCodes.Status201Created"/> response.
        /// </summary>
        /// <param name="routeName">The name of the route to use for generating the URL.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="value">The content value to format in the entity body.</param>
        /// <returns>The created <see cref="CreatedAtRouteResult"/> for the response.</returns>
        [NonAction]
        public virtual CreatedAtRouteResult CreatedAtRoute(string routeName, object routeValues, [ActionResultObjectValue] object value)
            => new CreatedAtRouteResult(routeName, routeValues, value);

        /// <summary>
        /// Creates a <see cref="AcceptedResult"/> object that produces an <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <returns>The created <see cref="AcceptedResult"/> for the response.</returns>
        [NonAction]
        public virtual AcceptedResult Accepted()
            => new AcceptedResult();

        /// <summary>
        /// Creates a <see cref="AcceptedResult"/> object that produces an <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="value">The optional content value to format in the entity body; may be null.</param>
        /// <returns>The created <see cref="AcceptedResult"/> for the response.</returns>
        [NonAction]
        public virtual AcceptedResult Accepted([ActionResultObjectValue] object value)
            => new AcceptedResult(location: null, value: value);

        /// <summary>
        /// Creates a <see cref="AcceptedResult"/> object that produces an <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="uri">The optional URI with the location at which the status of requested content can be monitored.
        /// May be null.</param>
        /// <returns>The created <see cref="AcceptedResult"/> for the response.</returns>
        [NonAction]
        public virtual AcceptedResult Accepted(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            return new AcceptedResult(locationUri: uri, value: null);
        }

        /// <summary>
        /// Creates a <see cref="AcceptedResult"/> object that produces an <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="uri">The optional URI with the location at which the status of requested content can be monitored.
        /// May be null.</param>
        /// <returns>The created <see cref="AcceptedResult"/> for the response.</returns>
        [NonAction]
        public virtual AcceptedResult Accepted(string uri)
            => new AcceptedResult(location: uri, value: null);

        /// <summary>
        /// Creates a <see cref="AcceptedResult"/> object that produces an <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="uri">The URI with the location at which the status of requested content can be monitored.</param>
        /// <param name="value">The optional content value to format in the entity body; may be null.</param>
        /// <returns>The created <see cref="AcceptedResult"/> for the response.</returns>
        [NonAction]
        public virtual AcceptedResult Accepted(string uri, [ActionResultObjectValue] object value)
            => new AcceptedResult(uri, value);

        /// <summary>
        /// Creates a <see cref="AcceptedResult"/> object that produces an <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="uri">The URI with the location at which the status of requested content can be monitored.</param>
        /// <param name="value">The optional content value to format in the entity body; may be null.</param>
        /// <returns>The created <see cref="AcceptedResult"/> for the response.</returns>
        [NonAction]
        public virtual AcceptedResult Accepted(Uri uri, [ActionResultObjectValue] object value)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            return new AcceptedResult(locationUri: uri, value: value);
        }

        /// <summary>
        /// Creates a <see cref="AcceptedAtActionResult"/> object that produces an <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="actionName">The name of the action to use for generating the URL.</param>
        /// <returns>The created <see cref="AcceptedAtActionResult"/> for the response.</returns>
        [NonAction]
        public virtual AcceptedAtActionResult AcceptedAtAction(string actionName)
            => AcceptedAtAction(actionName, routeValues: null, value: null);

        /// <summary>
        /// Creates a <see cref="AcceptedAtActionResult"/> object that produces an <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="actionName">The name of the action to use for generating the URL.</param>
        /// <param name="controllerName">The name of the controller to use for generating the URL.</param>
        /// <returns>The created <see cref="AcceptedAtActionResult"/> for the response.</returns>
        [NonAction]
        public virtual AcceptedAtActionResult AcceptedAtAction(string actionName, string controllerName)
            => AcceptedAtAction(actionName, controllerName, routeValues: null, value: null);

        /// <summary>
        /// Creates a <see cref="AcceptedAtActionResult"/> object that produces an <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="actionName">The name of the action to use for generating the URL.</param>
        /// <param name="value">The optional content value to format in the entity body; may be null.</param>
        /// <returns>The created <see cref="AcceptedAtActionResult"/> for the response.</returns>
        [NonAction]
        public virtual AcceptedAtActionResult AcceptedAtAction(string actionName, [ActionResultObjectValue] object value)
            => AcceptedAtAction(actionName, routeValues: null, value: value);

        /// <summary>
        /// Creates a <see cref="AcceptedAtActionResult"/> object that produces an <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="actionName">The name of the action to use for generating the URL.</param>
        /// <param name="controllerName">The name of the controller to use for generating the URL.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <returns>The created <see cref="AcceptedAtActionResult"/> for the response.</returns>
        [NonAction]
        public virtual AcceptedAtActionResult AcceptedAtAction(string actionName, string controllerName, [ActionResultObjectValue] object routeValues)
            => AcceptedAtAction(actionName, controllerName, routeValues, value: null);

        /// <summary>
        /// Creates a <see cref="AcceptedAtActionResult"/> object that produces an <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="actionName">The name of the action to use for generating the URL.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="value">The optional content value to format in the entity body; may be null.</param>
        /// <returns>The created <see cref="AcceptedAtActionResult"/> for the response.</returns>
        [NonAction]
        public virtual AcceptedAtActionResult AcceptedAtAction(string actionName, object routeValues, [ActionResultObjectValue] object value)
            => AcceptedAtAction(actionName, controllerName: null, routeValues: routeValues, value: value);

        /// <summary>
        /// Creates a <see cref="AcceptedAtActionResult"/> object that produces an <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="actionName">The name of the action to use for generating the URL.</param>
        /// <param name="controllerName">The name of the controller to use for generating the URL.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="value">The optional content value to format in the entity body; may be null.</param>
        /// <returns>The created <see cref="AcceptedAtActionResult"/> for the response.</returns>
        [NonAction]
        public virtual AcceptedAtActionResult AcceptedAtAction(
            string actionName,
            string controllerName,
            object routeValues,
            [ActionResultObjectValue] object value)
            => new AcceptedAtActionResult(actionName, controllerName, routeValues, value);

        /// <summary>
        /// Creates a <see cref="AcceptedAtRouteResult"/> object that produces an <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <returns>The created <see cref="AcceptedAtRouteResult"/> for the response.</returns>
        [NonAction]
        public virtual AcceptedAtRouteResult AcceptedAtRoute([ActionResultObjectValue] object routeValues)
            => AcceptedAtRoute(routeName: null, routeValues: routeValues, value: null);

        /// <summary>
        /// Creates a <see cref="AcceptedAtRouteResult"/> object that produces an <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="routeName">The name of the route to use for generating the URL.</param>
        /// <returns>The created <see cref="AcceptedAtRouteResult"/> for the response.</returns>
        [NonAction]
        public virtual AcceptedAtRouteResult AcceptedAtRoute(string routeName)
            => AcceptedAtRoute(routeName, routeValues: null, value: null);

        /// <summary>
        /// Creates a <see cref="AcceptedAtRouteResult"/> object that produces an <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="routeName">The name of the route to use for generating the URL.</param>
        ///<param name="routeValues">The route data to use for generating the URL.</param>
        /// <returns>The created <see cref="AcceptedAtRouteResult"/> for the response.</returns>
        [NonAction]
        public virtual AcceptedAtRouteResult AcceptedAtRoute(string routeName, object routeValues)
            => AcceptedAtRoute(routeName, routeValues, value: null);

        /// <summary>
        /// Creates a <see cref="AcceptedAtRouteResult"/> object that produces an <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="value">The optional content value to format in the entity body; may be null.</param>
        /// <returns>The created <see cref="AcceptedAtRouteResult"/> for the response.</returns>
        [NonAction]
        public virtual AcceptedAtRouteResult AcceptedAtRoute(object routeValues, [ActionResultObjectValue] object value)
            => AcceptedAtRoute(routeName: null, routeValues: routeValues, value: value);

        /// <summary>
        /// Creates a <see cref="AcceptedAtRouteResult"/> object that produces an <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="routeName">The name of the route to use for generating the URL.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="value">The optional content value to format in the entity body; may be null.</param>
        /// <returns>The created <see cref="AcceptedAtRouteResult"/> for the response.</returns>
        [NonAction]
        public virtual AcceptedAtRouteResult AcceptedAtRoute(string routeName, object routeValues, [ActionResultObjectValue] object value)
            => new AcceptedAtRouteResult(routeName, routeValues, value);

        /// <summary>
        /// Creates a <see cref="ChallengeResult"/>.
        /// </summary>
        /// <returns>The created <see cref="ChallengeResult"/> for the response.</returns>
        /// <remarks>
        /// The behavior of this method depends on the <see cref="IAuthenticationService"/> in use.
        /// <see cref="StatusCodes.Status401Unauthorized"/> and <see cref="StatusCodes.Status403Forbidden"/>
        /// are among likely status results.
        /// </remarks>
        [NonAction]
        public virtual ChallengeResult Challenge()
            => new ChallengeResult();

        /// <summary>
        /// Creates a <see cref="ChallengeResult"/> with the specified authentication schemes.
        /// </summary>
        /// <param name="authenticationSchemes">The authentication schemes to challenge.</param>
        /// <returns>The created <see cref="ChallengeResult"/> for the response.</returns>
        /// <remarks>
        /// The behavior of this method depends on the <see cref="IAuthenticationService"/> in use.
        /// <see cref="StatusCodes.Status401Unauthorized"/> and <see cref="StatusCodes.Status403Forbidden"/>
        /// are among likely status results.
        /// </remarks>
        [NonAction]
        public virtual ChallengeResult Challenge(params string[] authenticationSchemes)
            => new ChallengeResult(authenticationSchemes);

        /// <summary>
        /// Creates a <see cref="ChallengeResult"/> with the specified <paramref name="properties" />.
        /// </summary>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
        /// challenge.</param>
        /// <returns>The created <see cref="ChallengeResult"/> for the response.</returns>
        /// <remarks>
        /// The behavior of this method depends on the <see cref="IAuthenticationService"/> in use.
        /// <see cref="StatusCodes.Status401Unauthorized"/> and <see cref="StatusCodes.Status403Forbidden"/>
        /// are among likely status results.
        /// </remarks>
        [NonAction]
        public virtual ChallengeResult Challenge(AuthenticationProperties properties)
            => new ChallengeResult(properties);

        /// <summary>
        /// Creates a <see cref="ChallengeResult"/> with the specified authentication schemes and
        /// <paramref name="properties" />.
        /// </summary>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
        /// challenge.</param>
        /// <param name="authenticationSchemes">The authentication schemes to challenge.</param>
        /// <returns>The created <see cref="ChallengeResult"/> for the response.</returns>
        /// <remarks>
        /// The behavior of this method depends on the <see cref="IAuthenticationService"/> in use.
        /// <see cref="StatusCodes.Status401Unauthorized"/> and <see cref="StatusCodes.Status403Forbidden"/>
        /// are among likely status results.
        /// </remarks>
        [NonAction]
        public virtual ChallengeResult Challenge(
            AuthenticationProperties properties,
            params string[] authenticationSchemes)
            => new ChallengeResult(authenticationSchemes, properties);

        /// <summary>
        /// Creates a <see cref="ForbidResult"/> (<see cref="StatusCodes.Status403Forbidden"/> by default).
        /// </summary>
        /// <returns>The created <see cref="ForbidResult"/> for the response.</returns>
        /// <remarks>
        /// Some authentication schemes, such as cookies, will convert <see cref="StatusCodes.Status403Forbidden"/> to
        /// a redirect to show a login page.
        /// </remarks>
        [NonAction]
        public virtual ForbidResult Forbid()
            => new ForbidResult();

        /// <summary>
        /// Creates a <see cref="ForbidResult"/> (<see cref="StatusCodes.Status403Forbidden"/> by default) with the
        /// specified authentication schemes.
        /// </summary>
        /// <param name="authenticationSchemes">The authentication schemes to challenge.</param>
        /// <returns>The created <see cref="ForbidResult"/> for the response.</returns>
        /// <remarks>
        /// Some authentication schemes, such as cookies, will convert <see cref="StatusCodes.Status403Forbidden"/> to
        /// a redirect to show a login page.
        /// </remarks>
        [NonAction]
        public virtual ForbidResult Forbid(params string[] authenticationSchemes)
            => new ForbidResult(authenticationSchemes);

        /// <summary>
        /// Creates a <see cref="ForbidResult"/> (<see cref="StatusCodes.Status403Forbidden"/> by default) with the
        /// specified <paramref name="properties" />.
        /// </summary>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
        /// challenge.</param>
        /// <returns>The created <see cref="ForbidResult"/> for the response.</returns>
        /// <remarks>
        /// Some authentication schemes, such as cookies, will convert <see cref="StatusCodes.Status403Forbidden"/> to 
        /// a redirect to show a login page.
        /// </remarks>
        [NonAction]
        public virtual ForbidResult Forbid(AuthenticationProperties properties)
            => new ForbidResult(properties);

        /// <summary>
        /// Creates a <see cref="ForbidResult"/> (<see cref="StatusCodes.Status403Forbidden"/> by default) with the 
        /// specified authentication schemes and <paramref name="properties" />.
        /// </summary>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
        /// challenge.</param>
        /// <param name="authenticationSchemes">The authentication schemes to challenge.</param>
        /// <returns>The created <see cref="ForbidResult"/> for the response.</returns>
        /// <remarks>
        /// Some authentication schemes, such as cookies, will convert <see cref="StatusCodes.Status403Forbidden"/> to
        /// a redirect to show a login page.
        /// </remarks>
        [NonAction]
        public virtual ForbidResult Forbid(AuthenticationProperties properties, params string[] authenticationSchemes)
            => new ForbidResult(authenticationSchemes, properties);

        /// <summary>
        /// Creates a <see cref="SignInResult"/> with the specified authentication scheme.
        /// </summary>
        /// <param name="principal">The <see cref="ClaimsPrincipal"/> containing the user claims.</param>
        /// <param name="authenticationScheme">The authentication scheme to use for the sign-in operation.</param>
        /// <returns>The created <see cref="SignInResult"/> for the response.</returns>
        [NonAction]
        public virtual SignInResult SignIn(ClaimsPrincipal principal, string authenticationScheme)
            => new SignInResult(authenticationScheme, principal);

        /// <summary>
        /// Creates a <see cref="SignInResult"/> with the specified authentication scheme and
        /// <paramref name="properties" />.
        /// </summary>
        /// <param name="principal">The <see cref="ClaimsPrincipal"/> containing the user claims.</param>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the sign-in operation.</param>
        /// <param name="authenticationScheme">The authentication scheme to use for the sign-in operation.</param>
        /// <returns>The created <see cref="SignInResult"/> for the response.</returns>
        [NonAction]
        public virtual SignInResult SignIn(
            ClaimsPrincipal principal,
            AuthenticationProperties properties,
            string authenticationScheme)
            => new SignInResult(authenticationScheme, principal, properties);

        /// <summary>
        /// Creates a <see cref="SignOutResult"/> with the specified authentication schemes.
        /// </summary>
        /// <param name="authenticationSchemes">The authentication schemes to use for the sign-out operation.</param>
        /// <returns>The created <see cref="SignOutResult"/> for the response.</returns>
        [NonAction]
        public virtual SignOutResult SignOut(params string[] authenticationSchemes)
            => new SignOutResult(authenticationSchemes);

        /// <summary>
        /// Creates a <see cref="SignOutResult"/> with the specified authentication schemes and
        /// <paramref name="properties" />.
        /// </summary>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the sign-out operation.</param>
        /// <param name="authenticationSchemes">The authentication scheme to use for the sign-out operation.</param>
        /// <returns>The created <see cref="SignOutResult"/> for the response.</returns>
        [NonAction]
        public virtual SignOutResult SignOut(AuthenticationProperties properties, params string[] authenticationSchemes)
            => new SignOutResult(authenticationSchemes, properties);

        /// <summary>
        /// Updates the specified <paramref name="model"/> instance using values from the controller's current
        /// <see cref="IValueProvider"/>.
        /// </summary>
        /// <typeparam name="TModel">The type of the model object.</typeparam>
        /// <param name="model">The model instance to update.</param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful.</returns>
        [NonAction]
        public virtual Task<bool> TryUpdateModelAsync<TModel>(
            TModel model)
            where TModel : class
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return TryUpdateModelAsync(model, prefix: string.Empty);
        }

        /// <summary>
        /// Updates the specified <paramref name="model"/> instance using values from the controller's current
        /// <see cref="IValueProvider"/> and a <paramref name="prefix"/>.
        /// </summary>
        /// <typeparam name="TModel">The type of the model object.</typeparam>
        /// <param name="model">The model instance to update.</param>
        /// <param name="prefix">The prefix to use when looking up values in the current <see cref="IValueProvider"/>.
        /// </param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful.</returns>
        [NonAction]
        public virtual async Task<bool> TryUpdateModelAsync<TModel>(
            TModel model,
            string prefix)
            where TModel : class
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            var (success, valueProvider) = await CompositeValueProvider.TryCreateAsync(ControllerContext, ControllerContext.ValueProviderFactories);
            if (!success)
            {
                return false;
            }

            return await TryUpdateModelAsync(model, prefix, valueProvider);
        }

        /// <summary>
        /// Updates the specified <paramref name="model"/> instance using the <paramref name="valueProvider"/> and a
        /// <paramref name="prefix"/>.
        /// </summary>
        /// <typeparam name="TModel">The type of the model object.</typeparam>
        /// <param name="model">The model instance to update.</param>
        /// <param name="prefix">The prefix to use when looking up values in the <paramref name="valueProvider"/>.
        /// </param>
        /// <param name="valueProvider">The <see cref="IValueProvider"/> used for looking up values.</param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful.</returns>
        [NonAction]
        public virtual Task<bool> TryUpdateModelAsync<TModel>(
            TModel model,
            string prefix,
            IValueProvider valueProvider)
            where TModel : class
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            if (valueProvider == null)
            {
                throw new ArgumentNullException(nameof(valueProvider));
            }

            return ModelBindingHelper.TryUpdateModelAsync(
                model,
                prefix,
                ControllerContext,
                MetadataProvider,
                ModelBinderFactory,
                valueProvider,
                ObjectValidator);
        }

        /// <summary>
        /// Updates the specified <paramref name="model"/> instance using values from the controller's current
        /// <see cref="IValueProvider"/> and a <paramref name="prefix"/>.
        /// </summary>
        /// <typeparam name="TModel">The type of the model object.</typeparam>
        /// <param name="model">The model instance to update.</param>
        /// <param name="prefix">The prefix to use when looking up values in the current <see cref="IValueProvider"/>.
        /// </param>
        /// <param name="includeExpressions"> <see cref="Expression"/>(s) which represent top-level properties
        /// which need to be included for the current model.</param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful.</returns>
        [NonAction]
        public async Task<bool> TryUpdateModelAsync<TModel>(
            TModel model,
            string prefix,
            params Expression<Func<TModel, object>>[] includeExpressions)
           where TModel : class
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (includeExpressions == null)
            {
                throw new ArgumentNullException(nameof(includeExpressions));
            }

            var (success, valueProvider) = await CompositeValueProvider.TryCreateAsync(ControllerContext, ControllerContext.ValueProviderFactories);
            if (!success)
            {
                return false;
            }

            return await ModelBindingHelper.TryUpdateModelAsync(
                model,
                prefix,
                ControllerContext,
                MetadataProvider,
                ModelBinderFactory,
                valueProvider,
                ObjectValidator,
                includeExpressions);
        }

        /// <summary>
        /// Updates the specified <paramref name="model"/> instance using values from the controller's current
        /// <see cref="IValueProvider"/> and a <paramref name="prefix"/>.
        /// </summary>
        /// <typeparam name="TModel">The type of the model object.</typeparam>
        /// <param name="model">The model instance to update.</param>
        /// <param name="prefix">The prefix to use when looking up values in the current <see cref="IValueProvider"/>.
        /// </param>
        /// <param name="propertyFilter">A predicate which can be used to filter properties at runtime.</param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful.</returns>
        [NonAction]
        public async Task<bool> TryUpdateModelAsync<TModel>(
            TModel model,
            string prefix,
            Func<ModelMetadata, bool> propertyFilter)
            where TModel : class
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (propertyFilter == null)
            {
                throw new ArgumentNullException(nameof(propertyFilter));
            }

            var (success, valueProvider) = await CompositeValueProvider.TryCreateAsync(ControllerContext, ControllerContext.ValueProviderFactories);
            if (!success)
            {
                return false;
            }

            return await ModelBindingHelper.TryUpdateModelAsync(
                model,
                prefix,
                ControllerContext,
                MetadataProvider,
                ModelBinderFactory,
                valueProvider,
                ObjectValidator,
                propertyFilter);
        }

        /// <summary>
        /// Updates the specified <paramref name="model"/> instance using the <paramref name="valueProvider"/> and a
        /// <paramref name="prefix"/>.
        /// </summary>
        /// <typeparam name="TModel">The type of the model object.</typeparam>
        /// <param name="model">The model instance to update.</param>
        /// <param name="prefix">The prefix to use when looking up values in the <paramref name="valueProvider"/>.
        /// </param>
        /// <param name="valueProvider">The <see cref="IValueProvider"/> used for looking up values.</param>
        /// <param name="includeExpressions"> <see cref="Expression"/>(s) which represent top-level properties
        /// which need to be included for the current model.</param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful.</returns>
        [NonAction]
        public Task<bool> TryUpdateModelAsync<TModel>(
            TModel model,
            string prefix,
            IValueProvider valueProvider,
            params Expression<Func<TModel, object>>[] includeExpressions)
           where TModel : class
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (valueProvider == null)
            {
                throw new ArgumentNullException(nameof(valueProvider));
            }

            if (includeExpressions == null)
            {
                throw new ArgumentNullException(nameof(includeExpressions));
            }

            return ModelBindingHelper.TryUpdateModelAsync(
                model,
                prefix,
                ControllerContext,
                MetadataProvider,
                ModelBinderFactory,
                valueProvider,
                ObjectValidator,
                includeExpressions);
        }

        /// <summary>
        /// Updates the specified <paramref name="model"/> instance using the <paramref name="valueProvider"/> and a
        /// <paramref name="prefix"/>.
        /// </summary>
        /// <typeparam name="TModel">The type of the model object.</typeparam>
        /// <param name="model">The model instance to update.</param>
        /// <param name="prefix">The prefix to use when looking up values in the <paramref name="valueProvider"/>.
        /// </param>
        /// <param name="valueProvider">The <see cref="IValueProvider"/> used for looking up values.</param>
        /// <param name="propertyFilter">A predicate which can be used to filter properties at runtime.</param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful.</returns>
        [NonAction]
        public Task<bool> TryUpdateModelAsync<TModel>(
            TModel model,
            string prefix,
            IValueProvider valueProvider,
            Func<ModelMetadata, bool> propertyFilter)
            where TModel : class
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (valueProvider == null)
            {
                throw new ArgumentNullException(nameof(valueProvider));
            }

            if (propertyFilter == null)
            {
                throw new ArgumentNullException(nameof(propertyFilter));
            }

            return ModelBindingHelper.TryUpdateModelAsync(
                model,
                prefix,
                ControllerContext,
                MetadataProvider,
                ModelBinderFactory,
                valueProvider,
                ObjectValidator,
                propertyFilter);
        }

        /// <summary>
        /// Updates the specified <paramref name="model"/> instance using values from the controller's current
        /// <see cref="IValueProvider"/> and a <paramref name="prefix"/>.
        /// </summary>
        /// <param name="model">The model instance to update.</param>
        /// <param name="modelType">The type of model instance to update.</param>
        /// <param name="prefix">The prefix to use when looking up values in the current <see cref="IValueProvider"/>.
        /// </param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful.</returns>
        [NonAction]
        public virtual async Task<bool> TryUpdateModelAsync(
            object model,
            Type modelType,
            string prefix)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            var (success, valueProvider) = await CompositeValueProvider.TryCreateAsync(ControllerContext, ControllerContext.ValueProviderFactories);
            if (!success)
            {
                return false;
            }

            return await ModelBindingHelper.TryUpdateModelAsync(
                model,
                modelType,
                prefix,
                ControllerContext,
                MetadataProvider,
                ModelBinderFactory,
                valueProvider,
                ObjectValidator);
        }

        /// <summary>
        /// Updates the specified <paramref name="model"/> instance using the <paramref name="valueProvider"/> and a
        /// <paramref name="prefix"/>.
        /// </summary>
        /// <param name="model">The model instance to update.</param>
        /// <param name="modelType">The type of model instance to update.</param>
        /// <param name="prefix">The prefix to use when looking up values in the <paramref name="valueProvider"/>.
        /// </param>
        /// <param name="valueProvider">The <see cref="IValueProvider"/> used for looking up values.</param>
        /// <param name="propertyFilter">A predicate which can be used to filter properties at runtime.</param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful.</returns>
        [NonAction]
        public Task<bool> TryUpdateModelAsync(
            object model,
            Type modelType,
            string prefix,
            IValueProvider valueProvider,
            Func<ModelMetadata, bool> propertyFilter)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            if (valueProvider == null)
            {
                throw new ArgumentNullException(nameof(valueProvider));
            }

            if (propertyFilter == null)
            {
                throw new ArgumentNullException(nameof(propertyFilter));
            }

            return ModelBindingHelper.TryUpdateModelAsync(
                model,
                modelType,
                prefix,
                ControllerContext,
                MetadataProvider,
                ModelBinderFactory,
                valueProvider,
                ObjectValidator,
                propertyFilter);
        }

        /// <summary>
        /// Validates the specified <paramref name="model"/> instance.
        /// </summary>
        /// <param name="model">The model to validate.</param>
        /// <returns><c>true</c> if the <see cref="ModelState"/> is valid; <c>false</c> otherwise.</returns>
        [NonAction]
        public virtual bool TryValidateModel(
            object model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return TryValidateModel(model, prefix: null);
        }

        /// <summary>
        /// Validates the specified <paramref name="model"/> instance.
        /// </summary>
        /// <param name="model">The model to validate.</param>
        /// <param name="prefix">The key to use when looking up information in <see cref="ModelState"/>.
        /// </param>
        /// <returns><c>true</c> if the <see cref="ModelState"/> is valid;<c>false</c> otherwise.</returns>
        [NonAction]
        public virtual bool TryValidateModel(
            object model,
            string prefix)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            ObjectValidator.Validate(
                ControllerContext,
                validationState: null,
                prefix: prefix ?? string.Empty,
                model: model);
            return ModelState.IsValid;
        }
    }
}
