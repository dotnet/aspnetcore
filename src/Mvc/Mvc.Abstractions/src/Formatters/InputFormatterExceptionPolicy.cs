// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Formatters;

/// <summary>
/// Defines the set of policies that determine how the model binding system interprets exceptions
/// thrown by an <see cref="IInputFormatter"/>. <seealso cref="IInputFormatterExceptionPolicy"/>
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="IInputFormatter"/> could throw an exception for several reasons, including:
/// <list type="bullet">
/// <item><description>malformed input</description></item>
/// <item><description>client disconnect or other I/O problem</description></item>
/// <item><description>
/// application configuration problems such as <see cref="TypeLoadException"/>
/// </description></item>
/// </list>
/// </para>
/// <para>
/// The policy associated with <see cref="InputFormatterExceptionPolicy.AllExceptions"/> treats
/// all such categories of problems as model state errors, and usually will be reported to the client as
/// an HTTP 400. This was the only policy supported by model binding in ASP.NET Core MVC 1.0, 1.1, and 2.0
/// and is still the default for historical reasons.
/// </para>
/// <para>
/// The policy associated with <see cref="InputFormatterExceptionPolicy.MalformedInputExceptions"/>
/// treats only <see cref="InputFormatterException"/> and its subclasses as model state errors. This means that
/// exceptions that are not related to the content of the HTTP request (such as a disconnect) will be re-thrown,
/// which by default would cause an HTTP 500 response, unless there is exception-handling middleware enabled.
/// </para>
/// </remarks>
public enum InputFormatterExceptionPolicy
{
    /// <summary>
    /// This value indicates that all exceptions thrown by an <see cref="IInputFormatter"/> will be treated
    /// as model state errors.
    /// </summary>
    AllExceptions = 0,

    /// <summary>
    /// This value indicates that only <see cref="InputFormatterException"/> and subclasses will be treated
    /// as model state errors. All other exceptions types will be re-thrown and can be handled by a higher
    /// level exception handler, such as exception-handling middleware.
    /// </summary>
    MalformedInputExceptions = 1,
}
