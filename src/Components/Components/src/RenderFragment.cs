// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Represents a segment of UI content, implemented as a delegate that
    /// writes the content to a <see cref="RenderTreeBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="RenderTreeBuilder"/> to which the content should be written.</param>
    public delegate void RenderFragment(RenderTreeBuilder builder);

    /// <summary>
    /// Represents a segment of UI content for an object of type <typeparamref name="TValue"/>, implemented as
    /// a function that returns a <see cref="RenderFragment"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of object.</typeparam>
    /// <param name="value">The value used to build the content.</param>
    public delegate RenderFragment RenderFragment<TValue>(TValue value);
    /// <summary>
    /// Represents a segment of UI content for a pair of objects, implemented as a function that returns a <see cref="RenderFragment"/>.
    /// </summary>
    public delegate RenderFragment RenderFragment<T1, T2>(T1 arg1, T2 arg2);
    /// <summary>
    /// Represents a segment of UI content for a 3-tuple of objects, implemented as a function that returns a <see cref="RenderFragment"/>.
    /// </summary>
    public delegate RenderFragment RenderFragment<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3);
    /// <summary>
    /// Represents a segment of UI content for a 4-tuple of objects, implemented as a function that returns a <see cref="RenderFragment"/>.
    /// </summary>
    public delegate RenderFragment RenderFragment<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    /// <summary>
    /// Represents a segment of UI content for a 5-tuple of objects, implemented as a function that returns a <see cref="RenderFragment"/>.
    /// </summary>
    public delegate RenderFragment RenderFragment<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    /// <summary>
    /// Represents a segment of UI content for a 6-tuple of objects, implemented as a function that returns a <see cref="RenderFragment"/>.
    /// </summary>
    public delegate RenderFragment RenderFragment<T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
    /// <summary>
    /// Represents a segment of UI content for a 7-tuple of objects, implemented as a function that returns a <see cref="RenderFragment"/>.
    /// </summary>
    public delegate RenderFragment RenderFragment<T1, T2, T3, T4, T5, T6, T7>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
    /// <summary>
    /// Represents a segment of UI content for a 8-tuple of objects, implemented as a function that returns a <see cref="RenderFragment"/>.
    /// </summary>
    public delegate RenderFragment RenderFragment<T1, T2, T3, T4, T5, T6, T7, T8>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
    /// <summary>
    /// Represents a segment of UI content for a 9-tuple of objects, implemented as a function that returns a <see cref="RenderFragment"/>.
    /// </summary>
    public delegate RenderFragment RenderFragment<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);
    /// <summary>
    /// Represents a segment of UI content for a 10-tuple of objects, implemented as a function that returns a <see cref="RenderFragment"/>.
    /// </summary>
    public delegate RenderFragment RenderFragment<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);
    /// <summary>
    /// Represents a segment of UI content for a 11-tuple of objects, implemented as a function that returns a <see cref="RenderFragment"/>.
    /// </summary>
    public delegate RenderFragment RenderFragment<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11);
    /// <summary>
    /// Represents a segment of UI content for a 12-tuple of objects, implemented as a function that returns a <see cref="RenderFragment"/>.
    /// </summary>
    public delegate RenderFragment RenderFragment<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12);
    /// <summary>
    /// Represents a segment of UI content for a 13-tuple of objects, implemented as a function that returns a <see cref="RenderFragment"/>.
    /// </summary>
    public delegate RenderFragment RenderFragment<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13);
    /// <summary>
    /// Represents a segment of UI content for a 14-tuple of objects, implemented as a function that returns a <see cref="RenderFragment"/>.
    /// </summary>
    public delegate RenderFragment RenderFragment<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14);
    /// <summary>
    /// Represents a segment of UI content for a 15-tuple of objects, implemented as a function that returns a <see cref="RenderFragment"/>.
    /// </summary>
    public delegate RenderFragment RenderFragment<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15);
    /// <summary>
    /// Represents a segment of UI content for a 16-tuple of objects, implemented as a function that returns a <see cref="RenderFragment"/>.
    /// </summary>
    public delegate RenderFragment RenderFragment<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16);

}
