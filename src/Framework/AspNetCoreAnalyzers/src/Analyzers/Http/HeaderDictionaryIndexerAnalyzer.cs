// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.Http;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class HeaderDictionaryIndexerAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.UseHeaderDictionaryPropertiesInsteadOfIndexer);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(context =>
        {
            var propertyReference = (IPropertyReferenceOperation)context.Operation;
            var property = propertyReference.Property;

            // Check if property is the indexer on IHeaderDictionary, e.g. headers["content-type"]
            if (property.IsIndexer &&
                property.Parameters.Length == 1 &&
                property.Parameters[0].Type.SpecialType == SpecialType.System_String &&
                IsIHeadersDictionaryType(property.ContainingType))
            {
                // Get the indexer string argument.
                if (propertyReference.Arguments.Length == 1 &&
                    propertyReference.Arguments[0].Value is ILiteralOperation literalOperation &&
                    literalOperation.ConstantValue.Value is string indexerValue)
                {
                    // Check that the header has a matching property on IHeaderDictionary.
                    if (PropertyMapping.TryGetValue(indexerValue, out var propertyName))
                    {
                        AddDiagnosticWarning(context, propertyReference.Syntax.GetLocation(), indexerValue, propertyName);
                    }
                }
            }
        }, OperationKind.PropertyReference);
    }

    private static bool IsIHeadersDictionaryType(INamedTypeSymbol type)
    {
        // Only IHeaderDictionary is valid. Types like HeaderDictionary, which implement IHeaderDictionary,
        // can't access header properties unless cast as IHeaderDictionary.
        return type is
        {
            Name: "IHeaderDictionary",
            ContainingNamespace:
            {
                Name: "Http",
                ContainingNamespace:
                {
                    Name: "AspNetCore",
                    ContainingNamespace:
                    {
                        Name: "Microsoft",
                        ContainingNamespace:
                        {
                            IsGlobalNamespace: true
                        }
                    }
                }
            }
        };
    }

    // Internal for unit tests
    // Note that this dictionary should be kept in sync with properties in IHeaderDictionary.Keyed.cs
    // Key = property name, Value = header name
    internal static readonly Dictionary<string, string> PropertyMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Accept"] = "Accept",
        ["Accept-Charset"] = "AcceptCharset",
        ["Accept-Encoding"] = "AcceptEncoding",
        ["Accept-Language"] = "AcceptLanguage",
        ["Accept-Ranges"] = "AcceptRanges",
        ["Access-Control-Allow-Credentials"] = "AccessControlAllowCredentials",
        ["Access-Control-Allow-Headers"] = "AccessControlAllowHeaders",
        ["Access-Control-Allow-Methods"] = "AccessControlAllowMethods",
        ["Access-Control-Allow-Origin"] = "AccessControlAllowOrigin",
        ["Access-Control-Expose-Headers"] = "AccessControlExposeHeaders",
        ["Access-Control-Max-Age"] = "AccessControlMaxAge",
        ["Access-Control-Request-Headers"] = "AccessControlRequestHeaders",
        ["Access-Control-Request-Method"] = "AccessControlRequestMethod",
        ["Age"] = "Age",
        ["Allow"] = "Allow",
        ["Alt-Svc"] = "AltSvc",
        ["Authorization"] = "Authorization",
        ["baggage"] = "Baggage",
        ["Cache-Control"] = "CacheControl",
        ["Connection"] = "Connection",
        ["Content-Disposition"] = "ContentDisposition",
        ["Content-Encoding"] = "ContentEncoding",
        ["Content-Language"] = "ContentLanguage",
        ["Content-Location"] = "ContentLocation",
        ["Content-MD5"] = "ContentMD5",
        ["Content-Range"] = "ContentRange",
        ["Content-Security-Policy"] = "ContentSecurityPolicy",
        ["Content-Security-Policy-Report-Only"] = "ContentSecurityPolicyReportOnly",
        ["Content-Type"] = "ContentType",
        ["Correlation-Context"] = "CorrelationContext",
        ["Cookie"] = "Cookie",
        ["Date"] = "Date",
        ["ETag"] = "ETag",
        ["Expires"] = "Expires",
        ["Expect"] = "Expect",
        ["From"] = "From",
        ["Grpc-Accept-Encoding"] = "GrpcAcceptEncoding",
        ["Grpc-Encoding"] = "GrpcEncoding",
        ["Grpc-Message"] = "GrpcMessage",
        ["Grpc-Status"] = "GrpcStatus",
        ["Grpc-Timeout"] = "GrpcTimeout",
        ["Host"] = "Host",
        ["Keep-Alive"] = "KeepAlive",
        ["If-Match"] = "IfMatch",
        ["If-Modified-Since"] = "IfModifiedSince",
        ["If-None-Match"] = "IfNoneMatch",
        ["If-Range"] = "IfRange",
        ["If-Unmodified-Since"] = "IfUnmodifiedSince",
        ["Last-Modified"] = "LastModified",
        ["Link"] = "Link",
        ["Location"] = "Location",
        ["Max-Forwards"] = "MaxForwards",
        ["Origin"] = "Origin",
        ["Pragma"] = "Pragma",
        ["Proxy-Authenticate"] = "ProxyAuthenticate",
        ["Proxy-Authorization"] = "ProxyAuthorization",
        ["Proxy-Connection"] = "ProxyConnection",
        ["Range"] = "Range",
        ["Referer"] = "Referer",
        ["Retry-After"] = "RetryAfter",
        ["Request-Id"] = "RequestId",
        ["Sec-WebSocket-Accept"] = "SecWebSocketAccept",
        ["Sec-WebSocket-Key"] = "SecWebSocketKey",
        ["Sec-WebSocket-Protocol"] = "SecWebSocketProtocol",
        ["Sec-WebSocket-Version"] = "SecWebSocketVersion",
        ["Sec-WebSocket-Extensions"] = "SecWebSocketExtensions",
        ["Server"] = "Server",
        ["Set-Cookie"] = "SetCookie",
        ["Strict-Transport-Security"] = "StrictTransportSecurity",
        ["TE"] = "TE",
        ["Trailer"] = "Trailer",
        ["Transfer-Encoding"] = "TransferEncoding",
        ["Translate"] = "Translate",
        ["traceparent"] = "TraceParent",
        ["tracestate"] = "TraceState",
        ["Upgrade"] = "Upgrade",
        ["Upgrade-Insecure-Requests"] = "UpgradeInsecureRequests",
        ["User-Agent"] = "UserAgent",
        ["Vary"] = "Vary",
        ["Via"] = "Via",
        ["Warning"] = "Warning",
        ["WWW-Authenticate"] = "WWWAuthenticate",
        ["X-Content-Type-Options"] = "XContentTypeOptions",
        ["X-Frame-Options"] = "XFrameOptions",
        ["X-Powered-By"] = "XPoweredBy",
        ["X-Requested-With"] = "XRequestedWith",
        ["X-UA-Compatible"] = "XUACompatible",
        ["X-XSS-Protection"] = "XXSSProtection",
    };

    private static void AddDiagnosticWarning(OperationAnalysisContext context, Location location, string headerName, string propertyName)
    {
        var propertiesBuilder = ImmutableDictionary.CreateBuilder<string, string?>();
        propertiesBuilder.Add("HeaderName", headerName);
        propertiesBuilder.Add("ResolvedPropertyName", propertyName);

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.UseHeaderDictionaryPropertiesInsteadOfIndexer,
            location,
            propertiesBuilder.ToImmutable(),
            headerName,
            propertyName));
    }
}
