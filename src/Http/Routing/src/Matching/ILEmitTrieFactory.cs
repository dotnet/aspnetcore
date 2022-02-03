// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Routing.Matching;

internal static class ILEmitTrieFactory
{
    // The algorthm we use only works for ASCII text. If we find non-ASCII text in the input
    // we need to reject it and let is be processed with a fallback technique.
    public const int NotAscii = int.MinValue;

    // Creates a Func of (string path, int start, int length) => destination
    // Not using PathSegment here because we don't want to mess with visibility checks and
    // generating IL without it is easier.
    public static Func<string, int, int, int> Create(
        int defaultDestination,
        int exitDestination,
        (string text, int destination)[] entries,
        bool? vectorize)
    {
        var method = new DynamicMethod(
            "GetDestination",
            typeof(int),
            new[] { typeof(string), typeof(int), typeof(int), });

        GenerateMethodBody(method.GetILGenerator(), defaultDestination, exitDestination, entries, vectorize);

#if IL_EMIT_SAVE_ASSEMBLY
            SaveAssembly(method.GetILGenerator(), defaultDestination, exitDestination, entries, vectorize);
#endif

        return (Func<string, int, int, int>)method.CreateDelegate(typeof(Func<string, int, int, int>));
    }

    // Internal for testing
    internal static bool ShouldVectorize((string text, int destination)[] entries)
    {
        // There's no value in vectorizing the computation if we're on 32bit or
        // if no string is long enough. We do the vectorized comparison with uint64 ulongs
        // which isn't beneficial if they don't map to the native size of the CPU. The
        // vectorized algorithm introduces additional overhead for casing.

        // Vectorize by default on 64bit (allow override for testing)
        return (IntPtr.Size == 8) &&

        // Don't vectorize if all of the strings are small (prevents allocating unused locals)
        entries.Any(e => e.text.Length >= 4);
    }

    private static void GenerateMethodBody(
        ILGenerator il,
        int defaultDestination,
        int exitDestination,
        (string text, int destination)[] entries,
        bool? vectorize)
    {
        vectorize = vectorize ?? ShouldVectorize(entries);

        // See comments on Locals for details
        var locals = new Locals(il, vectorize.Value);

        // See comments on Labels for details
        var labels = new Labels()
        {
            ReturnDefault = il.DefineLabel(),
            ReturnNotAscii = il.DefineLabel(),
        };

        // See comments on Methods for details
        var methods = Methods.Instance;

        // Initializing top-level locals - this is similar to...
        // ReadOnlySpan<char> span = arg0.AsSpan(arg1, arg2);
        // ref byte p = ref Unsafe.As<char, byte>(MemoryMarshal.GetReference<char>(span))

        // arg0.AsSpan(arg1, arg2)
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Call, methods.AsSpan);

        // ReadOnlySpan<char> = ...
        il.Emit(OpCodes.Stloc, locals.Span);

        // MemoryMarshal.GetReference<char>(span)
        il.Emit(OpCodes.Ldloc, locals.Span);
        il.Emit(OpCodes.Call, methods.GetReference);

        // Unsafe.As<char, byte>(...)
        il.Emit(OpCodes.Call, methods.As);

        // ref byte p = ...
        il.Emit(OpCodes.Stloc_0, locals.P);

        var groups = entries.GroupBy(e => e.text.Length).ToArray();
        for (var i = 0; i < groups.Length; i++)
        {
            var group = groups[i];

            // Similar to 'if (length != X) { ... }
            var inside = il.DefineLabel();
            var next = il.DefineLabel();
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldc_I4, group.Key);
            il.Emit(OpCodes.Beq, inside);
            il.Emit(OpCodes.Br, next);

            // Process the group
            il.MarkLabel(inside);
            EmitTable(il, group.ToArray(), 0, group.Key, locals, labels, methods);
            il.MarkLabel(next);
        }

        // Exit point - we end up here when the text doesn't match
        il.MarkLabel(labels.ReturnDefault);
        il.Emit(OpCodes.Ldc_I4, defaultDestination);
        il.Emit(OpCodes.Ret);

        // Exit point - we end up here with the text contains non-ASCII text
        il.MarkLabel(labels.ReturnNotAscii);
        il.Emit(OpCodes.Ldc_I4, NotAscii);
        il.Emit(OpCodes.Ret);
    }

    private static void EmitTable(
        ILGenerator il,
        (string text, int destination)[] entries,
        int index,
        int length,
        Locals locals,
        Labels labels,
        Methods methods)
    {
        // We've reached the end of the string.
        if (index == length)
        {
            EmitReturnDestination(il, entries);
            return;
        }

        // If 4 or more characters remain, and we're vectorizing, we should process 4 characters at a time.
        if (length - index >= 4 && locals.UInt64Value != null)
        {
            EmitVectorizedTable(il, entries, index, length, locals, labels, methods);
            return;
        }

        // Fall back to processing a character at a time.
        EmitSingleCharacterTable(il, entries, index, length, locals, labels, methods);
    }

    private static void EmitVectorizedTable(
        ILGenerator il,
        (string text, int destination)[] entries,
        int index,
        int length,
        Locals locals,
        Labels labels,
        Methods methods)
    {
        // Emits code similar to:
        //
        // uint64Value = Unsafe.ReadUnaligned<ulong>(ref p);
        // p = ref Unsafe.Add(ref p, 8);
        //
        // if ((uint64Value & ~0x007F007F007F007FUL) == 0)
        // {
        //     return NotAscii;
        // }
        // uint64LowerIndicator = value + (0x0080008000800080UL - 0x0041004100410041UL);
        // uint64UpperIndicator = value + (0x0080008000800080UL - 0x005B005B005B005BUL);
        // ulong temp1 = uint64LowerIndicator ^ uint64UpperIndicator
        // ulong temp2 = temp1 & 0x0080008000800080UL;
        // ulong temp3 = (temp2) >> 2;
        // uint64Value = uint64Value ^ temp3;
        //
        // This is a vectorized non-branching technique for processing 4 utf16 characters
        // at a time inside a single uint64.
        //
        // Similar to:
        // https://github.com/GrabYourPitchforks/coreclr/commit/a3c1df25c4225995ffd6b18fd0fc39d6b81fd6a5#diff-d89b6ca07ea349899e45eed5f688a7ebR81
        //
        // Basically we need to check if the text is non-ASCII first and bail if it is.
        // The rest of the steps will convert the text to lowercase by checking all characters
        // at a time to see if they are in the A-Z range, that's where 0x0041 and 0x005B come in.

        // IMPORTANT
        //
        // If you are modifying this code, be aware that the easiest way to make a mistake is by
        // getting the set of casts wrong doing something like:
        //
        // il.Emit(OpCodes.Ldc_I8, ~0x007F007F007F007FUL);
        //
        // The IL Emit apis don't have overloads that accept ulong or ushort, and will resolve
        // an overload that does an undesirable conversion (for instance converting ulong to float).
        //
        // IMPORTANT

        // Unsafe.ReadUnaligned<ulong>(ref p)
        il.Emit(OpCodes.Ldloc, locals.P);
        il.Emit(OpCodes.Call, methods.ReadUnalignedUInt64);

        // uint64Value = ...
        il.Emit(OpCodes.Stloc, locals.UInt64Value);

        // Unsafe.Add(ref p, 8)
        il.Emit(OpCodes.Ldloc, locals.P);
        il.Emit(OpCodes.Ldc_I4, 8); // 8 bytes were read
        il.Emit(OpCodes.Call, methods.Add);

        // p = ref ...
        il.Emit(OpCodes.Stloc, locals.P);

        // if ((uint64Value & ~0x007F007F007F007FUL) == 0)
        // {
        //     goto: NotAscii;
        // }
        il.Emit(OpCodes.Ldloc, locals.UInt64Value);
        il.Emit(OpCodes.Ldc_I8, unchecked((long)~0x007F007F007F007FUL));
        il.Emit(OpCodes.And);
        il.Emit(OpCodes.Brtrue, labels.ReturnNotAscii);

        // uint64Value + (0x0080008000800080UL - 0x0041004100410041UL)
        il.Emit(OpCodes.Ldloc, locals.UInt64Value);
        il.Emit(OpCodes.Ldc_I8, unchecked((long)(0x0080008000800080UL - 0x0041004100410041UL)));
        il.Emit(OpCodes.Add);

        // uint64LowerIndicator = ...
        il.Emit(OpCodes.Stloc, locals.UInt64LowerIndicator);

        // value + (0x0080008000800080UL - 0x005B005B005B005BUL)
        il.Emit(OpCodes.Ldloc, locals.UInt64Value);
        il.Emit(OpCodes.Ldc_I8, unchecked((long)(0x0080008000800080UL - 0x005B005B005B005BUL)));
        il.Emit(OpCodes.Add);

        // uint64UpperIndicator = ...
        il.Emit(OpCodes.Stloc, locals.UInt64UpperIndicator);

        // ulongLowerIndicator ^ ulongUpperIndicator
        il.Emit(OpCodes.Ldloc, locals.UInt64LowerIndicator);
        il.Emit(OpCodes.Ldloc, locals.UInt64UpperIndicator);
        il.Emit(OpCodes.Xor);

        // ... & 0x0080008000800080UL
        il.Emit(OpCodes.Ldc_I8, unchecked((long)0x0080008000800080UL));
        il.Emit(OpCodes.And);

        // ... >> 2;
        il.Emit(OpCodes.Ldc_I4, 2);
        il.Emit(OpCodes.Shr_Un);

        // ...  ^ uint64Value
        il.Emit(OpCodes.Ldloc, locals.UInt64Value);
        il.Emit(OpCodes.Xor);

        // uint64Value = ...
        il.Emit(OpCodes.Stloc, locals.UInt64Value);

        // Now we generate an 'if' ladder with an entry for each of the unique 64 bit sections
        // of the text.
        var groups = entries.GroupBy(e => GetUInt64Key(e.text, index));
        foreach (var group in groups)
        {
            // if (uint64Value == 0x.....) { ... }
            var next = il.DefineLabel();
            il.Emit(OpCodes.Ldloc, locals.UInt64Value);
            il.Emit(OpCodes.Ldc_I8, unchecked((long)group.Key));
            il.Emit(OpCodes.Bne_Un, next);

            // Process the group
            EmitTable(il, group.ToArray(), index + 4, length, locals, labels, methods);
            il.MarkLabel(next);
        }

        // goto: defaultDestination
        il.Emit(OpCodes.Br, labels.ReturnDefault);
    }

    private static void EmitSingleCharacterTable(
        ILGenerator il,
        (string text, int destination)[] entries,
        int index,
        int length,
        Locals locals,
        Labels labels,
        Methods methods)
    {
        // See the vectorized code path for a much more thorough explanation.

        // IMPORTANT
        //
        // If you are modifying this code, be aware that the easiest way to make a mistake is by
        // getting the set of casts wrong doing something like:
        //
        // il.Emit(OpCodes.Ldc_I4, ~0x007F);
        //
        // The IL Emit apis don't have overloads that accept ulong or ushort, and will resolve
        // an overload that does an undesirable conversion (for instance convering ulong to float).
        //
        // IMPORTANT

        // Unsafe.ReadUnaligned<ushort>(ref p)
        il.Emit(OpCodes.Ldloc, locals.P);
        il.Emit(OpCodes.Call, methods.ReadUnalignedUInt16);

        // uint16Value = ...
        il.Emit(OpCodes.Stloc, locals.UInt16Value);

        // Unsafe.Add(ref p, 2)
        il.Emit(OpCodes.Ldloc, locals.P);
        il.Emit(OpCodes.Ldc_I4, 2); // 2 bytes were read
        il.Emit(OpCodes.Call, methods.Add);

        // p = ref ...
        il.Emit(OpCodes.Stloc, locals.P);

        // if ((uInt16Value & ~0x007FUL) == 0)
        // {
        //     goto: NotAscii;
        // }
        il.Emit(OpCodes.Ldloc, locals.UInt16Value);
        il.Emit(OpCodes.Ldc_I4, unchecked((int)((uint)~0x007F)));
        il.Emit(OpCodes.And);
        il.Emit(OpCodes.Brtrue, labels.ReturnNotAscii);

        // Since we're handling a single character at a time, it's easier to just
        // generate an 'if' with two comparisons instead of doing complicated conversion
        // logic.

        // Now we generate an 'if' ladder with an entry for each of the unique
        // characters in the group.
        var groups = entries.GroupBy(e => GetUInt16Key(e.text, index));
        foreach (var group in groups)
        {
            // if (uInt16Value == 'A' || uint16Value == 'a') { ... }
            var next = il.DefineLabel();
            var inside = il.DefineLabel();
            il.Emit(OpCodes.Ldloc, locals.UInt16Value);
            il.Emit(OpCodes.Ldc_I4, unchecked((int)((uint)group.Key)));
            il.Emit(OpCodes.Beq, inside);

            var upper = (ushort)char.ToUpperInvariant((char)group.Key);
            if (upper != group.Key)
            {
                il.Emit(OpCodes.Ldloc, locals.UInt16Value);
                il.Emit(OpCodes.Ldc_I4, unchecked((int)((uint)upper)));
                il.Emit(OpCodes.Beq, inside);
            }

            il.Emit(OpCodes.Br, next);

            // Process the group
            il.MarkLabel(inside);
            EmitTable(il, group.ToArray(), index + 1, length, locals, labels, methods);
            il.MarkLabel(next);
        }

        // goto: defaultDestination
        il.Emit(OpCodes.Br, labels.ReturnDefault);
    }

    public static void EmitReturnDestination(ILGenerator il, (string text, int destination)[] entries)
    {
        Debug.Assert(entries.Length == 1, "We should have a single entry");
        il.Emit(OpCodes.Ldc_I4, entries[0].destination);
        il.Emit(OpCodes.Ret);
    }

    private static ulong GetUInt64Key(string text, int index)
    {
        Debug.Assert(index + 4 <= text.Length);
        var span = text.ToLowerInvariant().AsSpan(index);
        ref var p = ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(span));
        return Unsafe.ReadUnaligned<ulong>(ref p);
    }

    private static ushort GetUInt16Key(string text, int index)
    {
        Debug.Assert(index + 1 <= text.Length);
        return (ushort)char.ToLowerInvariant(text[index]);
    }

    // We require a special build-time define since this is a testing/debugging
    // feature that will litter the app directory with assemblies.
#if IL_EMIT_SAVE_ASSEMBLY
        private static void SaveAssembly(
            int defaultDestination,
            int exitDestination,
            (string text, int destination)[] entries,
            bool? vectorize)
        {
            var assemblyName = "Microsoft.AspNetCore.Routing.ILEmitTrie" + DateTime.Now.Ticks;
            var fileName = assemblyName + ".dll";
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.RunAndSave);
            var module = assembly.DefineDynamicModule(assemblyName, fileName);
            var type = module.DefineType("ILEmitTrie");
            var method = type.DefineMethod(
                "GetDestination",
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                typeof(int),
                new [] { typeof(string), typeof(int), typeof(int), };

            GenerateMethodBody(method.GetILGenerator(), defaultDestination, exitDestination, entries, vectorize);

            type.CreateTypeInfo();
            assembly.Save(fileName);
        }
#endif

    private class Locals
    {
        public Locals(ILGenerator il, bool vectorize)
        {
            P = il.DeclareLocal(typeof(byte).MakeByRefType());
            Span = il.DeclareLocal(typeof(ReadOnlySpan<char>));

            UInt16Value = il.DeclareLocal(typeof(ushort));

            if (vectorize)
            {
                UInt64Value = il.DeclareLocal(typeof(ulong));
                UInt64LowerIndicator = il.DeclareLocal(typeof(ulong));
                UInt64UpperIndicator = il.DeclareLocal(typeof(ulong));
            }
        }

        /// <summary>
        /// Holds current character when processing a character at a time.
        /// </summary>
        public LocalBuilder UInt16Value { get; }

        /// <summary>
        /// Holds current character when processing 4 characters at a time.
        /// </summary>
        public LocalBuilder UInt64Value { get; }

        /// <summary>
        /// Used to covert casing. See comments where it's used.
        /// </summary>
        public LocalBuilder UInt64LowerIndicator { get; }

        /// <summary>
        /// Used to covert casing. See comments where it's used.
        /// </summary>
        public LocalBuilder UInt64UpperIndicator { get; }

        /// <summary>
        /// Holds a 'ref byte' reference to the current character (in bytes).
        /// </summary>
        public LocalBuilder P { get; }

        /// <summary>
        /// Holds the relevant portion of the path as a Span[byte].
        /// </summary>
        public LocalBuilder Span { get; }
    }

    private class Labels
    {
        /// <summary>
        /// Label to goto that will return the default destination (not a match).
        /// </summary>
        public Label ReturnDefault { get; set; }

        /// <summary>
        /// Label to goto that will return a sentinel value for non-ascii text.
        /// </summary>
        public Label ReturnNotAscii { get; set; }
    }

    private class Methods
    {
        // Caching because the methods won't change, if we're being called once we're likely to
        // be called again.
        public static readonly Methods Instance = new Methods();

        private Methods()
        {
            // Can't use GetMethod because the parameter is a generic method parameters.
            Add = typeof(Unsafe)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == nameof(Unsafe.Add))
                .Where(m => m.GetGenericArguments().Length == 1)
                .Where(m => m.GetParameters().Length == 2)
                .FirstOrDefault()
                ?.MakeGenericMethod(typeof(byte));
            if (Add == null)
            {
                throw new InvalidOperationException("Failed to find Unsafe.Add{T}(ref T, int)");
            }

            // Can't use GetMethod because the parameter is a generic method parameters.
            As = typeof(Unsafe)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == nameof(Unsafe.As))
                .Where(m => m.GetGenericArguments().Length == 2)
                .Where(m => m.GetParameters().Length == 1)
                .FirstOrDefault()
                ?.MakeGenericMethod(typeof(char), typeof(byte));
            if (Add == null)
            {
                throw new InvalidOperationException("Failed to find Unsafe.As{TFrom, TTo}(ref TFrom)");
            }

            AsSpan = typeof(MemoryExtensions).GetMethod(
                nameof(MemoryExtensions.AsSpan),
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                new[] { typeof(string), typeof(int), typeof(int), },
                modifiers: null);
            if (AsSpan == null)
            {
                throw new InvalidOperationException("Failed to find MemoryExtensions.AsSpan(string, int, int)");
            }

            // Can't use GetMethod because the parameter is a generic method parameters.
            GetReference = typeof(MemoryMarshal)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == nameof(MemoryMarshal.GetReference))
                .Where(m => m.GetGenericArguments().Length == 1)
                .Where(m => m.GetParameters().Length == 1)
                // Disambiguate between ReadOnlySpan<> and Span<> - this method is overloaded.
                .Where(m => m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(ReadOnlySpan<>))
                .FirstOrDefault()
                ?.MakeGenericMethod(typeof(char));
            if (GetReference == null)
            {
                throw new InvalidOperationException("Failed to find MemoryMarshal.GetReference{T}(ReadOnlySpan{T})");
            }

            ReadUnalignedUInt64 = typeof(Unsafe).GetMethod(
                nameof(Unsafe.ReadUnaligned),
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                new[] { typeof(byte).MakeByRefType(), },
                modifiers: null)
                .MakeGenericMethod(typeof(ulong));
            if (ReadUnalignedUInt64 == null)
            {
                throw new InvalidOperationException("Failed to find Unsafe.ReadUnaligned{T}(ref byte)");
            }

            ReadUnalignedUInt16 = typeof(Unsafe).GetMethod(
                nameof(Unsafe.ReadUnaligned),
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                new[] { typeof(byte).MakeByRefType(), },
                modifiers: null)
                .MakeGenericMethod(typeof(ushort));
            if (ReadUnalignedUInt16 == null)
            {
                throw new InvalidOperationException("Failed to find Unsafe.ReadUnaligned{T}(ref byte)");
            }
        }

        /// <summary>
        /// <see cref="Unsafe.Add{T}(ref T, int)"/> - Add[ref byte]
        /// </summary>
        public MethodInfo Add { get; }

        /// <summary>
        /// <see cref="Unsafe.As{TFrom, TTo}(ref TFrom)"/> - As[char, byte]
        /// </summary>
        public MethodInfo As { get; }

        /// <summary>
        /// <see cref="MemoryExtensions.AsSpan(string, int, int)"/>
        /// </summary>
        public MethodInfo AsSpan { get; }

        /// <summary>
        /// <see cref="MemoryMarshal.GetReference{T}(ReadOnlySpan{T})"/> - GetReference[char]
        /// </summary>
        public MethodInfo GetReference { get; }

        /// <summary>
        /// <see cref="Unsafe.ReadUnaligned{T}(ref byte)"/> - ReadUnaligned[ulong]
        /// </summary>
        public MethodInfo ReadUnalignedUInt64 { get; }

        /// <summary>
        /// <see cref="Unsafe.ReadUnaligned{T}(ref byte)"/> - ReadUnaligned[ushort]
        /// </summary>
        public MethodInfo ReadUnalignedUInt16 { get; }
    }
}
