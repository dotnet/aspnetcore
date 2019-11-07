// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Hosting
{
    public static partial class WebHostBuilderHttpSysExtensions
    {
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder UseHttpSys(this Microsoft.AspNetCore.Hosting.IWebHostBuilder hostBuilder) { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder UseHttpSys(this Microsoft.AspNetCore.Hosting.IWebHostBuilder hostBuilder, System.Action<Microsoft.AspNetCore.Server.HttpSys.HttpSysOptions> options) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Server.HttpSys
{
    public sealed partial class AuthenticationManager
    {
        internal AuthenticationManager() { }
        public bool AllowAnonymous { get { throw null; } set { } }
        public bool AutomaticAuthentication { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Server.HttpSys.AuthenticationSchemes Schemes { get { throw null; } set { } }
    }
    [System.FlagsAttribute]
    public enum AuthenticationSchemes
    {
        None = 0,
        Basic = 1,
        NTLM = 4,
        Negotiate = 8,
        Kerberos = 16,
    }
    public enum ClientCertificateMethod
    {
        NoCertificate = 0,
        AllowCertificate = 1,
        AllowRenegotation = 2,
    }
    public enum Http503VerbosityLevel : long
    {
        Basic = (long)0,
        Limited = (long)1,
        Full = (long)2,
    }
    public static partial class HttpSysDefaults
    {
        public const string AuthenticationScheme = "Windows";
    }
    public partial class HttpSysException : System.ComponentModel.Win32Exception
    {
        internal HttpSysException() { }
        public override int ErrorCode { get { throw null; } }
    }
    public partial class HttpSysOptions
    {
        public HttpSysOptions() { }
        public bool AllowSynchronousIO { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Server.HttpSys.AuthenticationManager Authentication { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Server.HttpSys.ClientCertificateMethod ClientCertificateMethod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool EnableResponseCaching { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Server.HttpSys.Http503VerbosityLevel Http503Verbosity { get { throw null; } set { } }
        public int MaxAccepts { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public long? MaxConnections { get { throw null; } set { } }
        public long? MaxRequestBodySize { get { throw null; } set { } }
        public long RequestQueueLimit { get { throw null; } set { } }
        public Microsoft.AspNetCore.Server.HttpSys.RequestQueueMode RequestQueueMode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string RequestQueueName { get { throw null; } set { } }
        public bool ThrowWriteExceptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Server.HttpSys.TimeoutManager Timeouts { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Server.HttpSys.UrlPrefixCollection UrlPrefixes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial interface IHttpSysRequestInfoFeature
    {
        System.Collections.Generic.IReadOnlyDictionary<int, System.ReadOnlyMemory<byte>> RequestInfo { get; }
    }
    public enum RequestQueueMode
    {
        Create = 0,
        Attach = 1,
        CreateOrAttach = 2,
    }
    public sealed partial class TimeoutManager
    {
        internal TimeoutManager() { }
        public System.TimeSpan DrainEntityBody { get { throw null; } set { } }
        public System.TimeSpan EntityBody { get { throw null; } set { } }
        public System.TimeSpan HeaderWait { get { throw null; } set { } }
        public System.TimeSpan IdleConnection { get { throw null; } set { } }
        public long MinSendBytesPerSecond { get { throw null; } set { } }
        public System.TimeSpan RequestQueue { get { throw null; } set { } }
    }
    public partial class UrlPrefix
    {
        internal UrlPrefix() { }
        public string FullPrefix { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Host { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool IsHttps { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Path { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Port { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public int PortValue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Scheme { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static Microsoft.AspNetCore.Server.HttpSys.UrlPrefix Create(string prefix) { throw null; }
        public static Microsoft.AspNetCore.Server.HttpSys.UrlPrefix Create(string scheme, string host, int? portValue, string path) { throw null; }
        public static Microsoft.AspNetCore.Server.HttpSys.UrlPrefix Create(string scheme, string host, string port, string path) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class UrlPrefixCollection : System.Collections.Generic.ICollection<Microsoft.AspNetCore.Server.HttpSys.UrlPrefix>, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Server.HttpSys.UrlPrefix>, System.Collections.IEnumerable
    {
        internal UrlPrefixCollection() { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public void Add(Microsoft.AspNetCore.Server.HttpSys.UrlPrefix item) { }
        public void Add(string prefix) { }
        public void Clear() { }
        public bool Contains(Microsoft.AspNetCore.Server.HttpSys.UrlPrefix item) { throw null; }
        public void CopyTo(Microsoft.AspNetCore.Server.HttpSys.UrlPrefix[] array, int arrayIndex) { }
        public System.Collections.Generic.IEnumerator<Microsoft.AspNetCore.Server.HttpSys.UrlPrefix> GetEnumerator() { throw null; }
        public bool Remove(Microsoft.AspNetCore.Server.HttpSys.UrlPrefix item) { throw null; }
        public bool Remove(string prefix) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
}
