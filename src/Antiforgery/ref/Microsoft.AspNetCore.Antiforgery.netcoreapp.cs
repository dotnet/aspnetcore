// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Antiforgery
{
    public partial class AntiforgeryOptions
    {
        public static readonly string DefaultCookiePrefix;
        public AntiforgeryOptions() { }
        public Microsoft.AspNetCore.Http.CookieBuilder Cookie { get { throw null; } set { } }
        public string FormFieldName { get { throw null; } set { } }
        public string HeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool SuppressXFrameOptionsHeader { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class AntiforgeryTokenSet
    {
        public AntiforgeryTokenSet(string requestToken, string cookieToken, string formFieldName, string headerName) { }
        public string CookieToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string FormFieldName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string HeaderName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string RequestToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class AntiforgeryValidationException : System.Exception
    {
        public AntiforgeryValidationException(string message) { }
        public AntiforgeryValidationException(string message, System.Exception innerException) { }
    }
    public partial interface IAntiforgery
    {
        Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet GetAndStoreTokens(Microsoft.AspNetCore.Http.HttpContext httpContext);
        Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet GetTokens(Microsoft.AspNetCore.Http.HttpContext httpContext);
        System.Threading.Tasks.Task<bool> IsRequestValidAsync(Microsoft.AspNetCore.Http.HttpContext httpContext);
        void SetCookieTokenAndHeader(Microsoft.AspNetCore.Http.HttpContext httpContext);
        System.Threading.Tasks.Task ValidateRequestAsync(Microsoft.AspNetCore.Http.HttpContext httpContext);
    }
    public partial interface IAntiforgeryAdditionalDataProvider
    {
        string GetAdditionalData(Microsoft.AspNetCore.Http.HttpContext context);
        bool ValidateAdditionalData(Microsoft.AspNetCore.Http.HttpContext context, string additionalData);
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class AntiforgeryServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAntiforgery(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAntiforgery(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions> setupAction) { throw null; }
    }
}
