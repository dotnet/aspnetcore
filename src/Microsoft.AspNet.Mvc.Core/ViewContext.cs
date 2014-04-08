using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public class ViewContext
    {
        private DynamicViewData _viewBag;

        // We need a default FormContext if the user uses html <form> instead of an MvcForm
        private readonly FormContext _defaultFormContext = new FormContext();

        private FormContext _formContext;
        
        public ViewContext([NotNull] ViewContext viewContext)
            : this(viewContext.ServiceProvider, viewContext.HttpContext, viewContext.ViewEngineContext)
        {
            UnobtrusiveJavaScriptEnabled = viewContext.UnobtrusiveJavaScriptEnabled;
            ClientValidationEnabled = viewContext.ClientValidationEnabled;
        }

        public ViewContext(IServiceProvider serviceProvider, HttpContext httpContext,
            IDictionary<string, object> viewEngineContext)
        {
            ServiceProvider = serviceProvider;
            HttpContext = httpContext;
            ViewEngineContext = viewEngineContext;
            _formContext = _defaultFormContext;
            UnobtrusiveJavaScriptEnabled = true;
            ClientValidationEnabled = true;
        }

        public virtual FormContext FormContext
        {
            get
            {
                return _formContext;
            }
            set
            {
                // Never return a null form context, this is important for validation purposes.
                _formContext = value ?? _defaultFormContext;
            }
        }

        public HttpContext HttpContext { get; private set; }

        public IServiceProvider ServiceProvider { get; private set; }

        public bool UnobtrusiveJavaScriptEnabled { get; set; }

        public bool ClientValidationEnabled { get; set; }

        public dynamic ViewBag
        {
            get
            {
                if (_viewBag == null)
                {
                    _viewBag = new DynamicViewData(() => ViewData);
                }

                return _viewBag;
            }
        }

        public ViewDataDictionary ViewData { get; set; }

        public IDictionary<string, object> ViewEngineContext { get; private set; }

        public TextWriter Writer { get; set; }
    }
}
