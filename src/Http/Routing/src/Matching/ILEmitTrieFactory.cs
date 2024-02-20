// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Routing.Matching;

[RequiresDynamicCode("ILEmitTrieFactory uses runtime IL generation.")]
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

        GenerateMethodBody(method.GetILGenerator(), defaultDestination, entries, vectorize);

#if IL_EMIT_SAVE_ASSEMBLY
            SaveAssembly(method.GetILGenerator(), defaultDestination, entries, vectorize);
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

        const int binarySearchThreshold = 4; // The number of items above which it makes sense to binary search
        var groups = entries.GroupBy(e => e.text.Length).ToArray();

        if (groups.Length >= binarySearchThreshold)
        {
            // Only sort if binary search will be used.
            Array.Sort(groups, static (a, b) => a.Key.CompareTo(b.Key));
        }

        EmitIfLadder(groups);

        // Exit point - we end up here when the text doesn't match
        il.MarkLabel(labels.ReturnDefault);
        il.Emit(OpCodes.Ldc_I4, defaultDestination);
        il.Emit(OpCodes.Ret);

        // Exit point - we end up here with the text contains non-ASCII text
        il.MarkLabel(labels.ReturnNotAscii);
        il.Emit(OpCodes.Ldc_I4, NotAscii);
        il.Emit(OpCodes.Ret);

        void EmitIfLadder(Span<IGrouping<int, (string text, int destination)>> groups)
        {
            if (groups.Length < binarySearchThreshold)
            {
                // Use sequential if statements, starting from the most common length
                groups.Sort(static (a, b) => b.Count().CompareTo(a.Count()));
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
            }
            else
            {
                // Use binary search tree
                var mid = groups.Length / 2;

                var rightBranch = il.DefineLabel();
                var next = il.DefineLabel();

                // if (length < X) { ... }
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldc_I4, groups[mid].Key);
                il.Emit(OpCodes.Bge, rightBranch);

                EmitIfLadder(groups[..mid]);
                il.Emit(OpCodes.Br, next);

                // else { ... }
                il.MarkLabel(rightBranch);
                EmitIfLadder(groups[mid..]);

                il.MarkLabel(next);
            }
        }
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

        if (entries.All(e => IsUInt64KeyAsciiLettersOnly(e.text, index)))
        {
            // Here we know that all characters in our keys will all be in the set [a-z]
            // If we set all the 0x20 bit in the input text, then it does not matter
            // if we are incorrectly changing e.g. @ to `, as it won't match our letters
            // anyway. In fact, since we know that the target set is [a-z] then the only
            // characters to match our target set after having their 0x20 bit set are...
            // [A-Z] which is exactly what we want to achieve.

            // uint64Value | 0x0020002000200020UL
            il.Emit(OpCodes.Ldloc, locals.UInt64Value);
            il.Emit(OpCodes.Ldc_I8, unchecked((long)0x0020002000200020UL));
            il.Emit(OpCodes.Or);

            // uint64Value = ...
            il.Emit(OpCodes.Stloc, locals.UInt64Value);
        }
        else
        {
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

            // uint64LowerIndicator ^ uint64UpperIndicator
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
        }

        const int binarySearchThreshold = 4; // The number of items above which it makes sense to binary search

        var groups = entries.GroupBy(e => GetUInt64Key(e.text, index)).ToArray();

        if (groups.Length >= binarySearchThreshold)
        {
            // Only sort if binary search will be used.
            Array.Sort(groups, static (a, b) => unchecked((long)a.Key).CompareTo(unchecked((long)b.Key)));
        }

        EmitIfLadder(groups);

        // goto: defaultDestination
        il.Emit(OpCodes.Br, labels.ReturnDefault);

        void EmitIfLadder(Span<IGrouping<ulong, (string test, int destination)>> groups)
        {
            // Generate an 'if' ladder with an entry for each of the unique 64 bit sections of the text.

            if (groups.Length < binarySearchThreshold)
            {
                // Use sequential if statements, starting from the most common segment
                groups.Sort(static (a, b) => b.Count().CompareTo(a.Count()));
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
            }
            else
            {
                // Use binary search tree
                var mid = groups.Length / 2;

                var rightBranch = il.DefineLabel();
                var nextGroup = il.DefineLabel();

                // if (uint64Value < 0x.....) { ... }
                il.Emit(OpCodes.Ldloc, locals.UInt64Value);
                il.Emit(OpCodes.Ldc_I8, unchecked((long)groups[mid].Key));
                il.Emit(OpCodes.Bge_Un, rightBranch);

                EmitIfLadder(groups[..mid]);
                il.Emit(OpCodes.Br, nextGroup);

                // else { ... }
                il.MarkLabel(rightBranch);
                EmitIfLadder(groups[mid..]);

                il.MarkLabel(nextGroup);
            }
        }
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

        // if ((uint16Value & ~0x007FUL) == 0)
        // {
        //     goto: NotAscii;
        // }
        il.Emit(OpCodes.Ldloc, locals.UInt16Value);
        il.Emit(OpCodes.Ldc_I4, unchecked((int)((uint)~0x007F)));
        il.Emit(OpCodes.And);
        il.Emit(OpCodes.Brtrue, labels.ReturnNotAscii);

        // uint16Value | 0x20
        il.Emit(OpCodes.Ldloc, locals.UInt16Value);
        il.Emit(OpCodes.Ldc_I4, 0x20);
        il.Emit(OpCodes.Or);

        // uint16ValueLowerCase = ...
        il.Emit(OpCodes.Stloc, locals.UInt16ValueLowerCase);

        const int binarySearchThreshold = 4; // The number of items above which it makes sense to binary search

        // Now we generate an 'if' ladder with an entry for each of the unique
        // characters in the group.
        var groups = entries.GroupBy(e => GetUInt16Key(e.text, index)).ToArray();

        // We can still apply binary search in most cases. Specifically when besides letters,
        // there are no two keys which are equal aside from the 0x20 bit. Reasoning is that
        // if that's the case we can binary search on the (|0x20) version anyway and finally
        // compare equality with the correct version.
        var disableBinarySearch = groups.Any(group => groups.Any(otherGroup => otherGroup.Key != group.Key && (otherGroup.Key | 0x20) == (group.Key | 0x20)));

        if (!disableBinarySearch && groups.Length >= binarySearchThreshold)
        {
            // Only sort if binary search will be used. Sort by the "lower" value - even if not a letter
            // as with disableBinarySearch we have confirmed that no two keys will conflict.
            Array.Sort(groups, static (a, b) => (a.Key | 0x20).CompareTo(b.Key | 0x20));
        }

        EmitIfLadder(groups);

        // goto: defaultDestination
        il.Emit(OpCodes.Br, labels.ReturnDefault);

        void EmitIfLadder(Span<IGrouping<ushort, (string test, int destination)>> groups)
        {
            // Generate an 'if' ladder with an entry for each of the unique 64 bit sections of the text.

            if (disableBinarySearch || groups.Length < binarySearchThreshold)
            {
                // Use sequential if statements, starting from the most common segment
                groups.Sort(static (a, b) => b.Count().CompareTo(a.Count()));

                foreach (var group in groups)
                {
                    // Choose which variable against which to compare.
                    var comparisonLocal = group.Key >= 'a' && group.Key <= 'z'
                        ? locals.UInt16ValueLowerCase
                        : locals.UInt16Value;

                    // if (uint16Value/uint16ValueLowerCase == 'a') { ... }
                    var next = il.DefineLabel();
                    il.Emit(OpCodes.Ldloc, comparisonLocal);
                    il.Emit(OpCodes.Ldc_I4, unchecked((int)(uint)group.Key));
                    il.Emit(OpCodes.Ceq);
                    il.Emit(OpCodes.Brfalse, next);

                    // Process the group
                    EmitTable(il, group.ToArray(), index + 1, length, locals, labels, methods);
                    il.MarkLabel(next);
                }
            }
            else
            {
                // Use binary search tree
                var mid = groups.Length / 2;

                var rightBranch = il.DefineLabel();
                var nextGroup = il.DefineLabel();

                // if (uint16ValueLowerVase < 0x.....) { ... }
                il.Emit(OpCodes.Ldloc, locals.UInt16ValueLowerCase);
                il.Emit(OpCodes.Ldc_I4, unchecked(((int)(uint)groups[mid].Key | 0x20)));
                il.Emit(OpCodes.Bge_Un, rightBranch);

                EmitIfLadder(groups[..mid]);
                il.Emit(OpCodes.Br, nextGroup);

                // else { ... }
                il.MarkLabel(rightBranch);
                EmitIfLadder(groups[mid..]);

                il.MarkLabel(nextGroup);
            }
        }
    }

    public static void EmitReturnDestination(ILGenerator il, (string text, int destination)[] entries)
    {
        Debug.Assert(entries.Length == 1, "We should have a single entry");
        il.Emit(OpCodes.Ldc_I4, entries[0].destination);
        il.Emit(OpCodes.Ret);
    }

    /// <summary>
    /// Returns true if the key will only contains ascii letters
    /// </summary>
    private static bool IsUInt64KeyAsciiLettersOnly(string text, int index)
    {
        Debug.Assert(index + 4 <= text.Length);
        var span = text.AsSpan(index, 4);
        return char.IsAsciiLetter(span[0])
            && char.IsAsciiLetter(span[1])
            && char.IsAsciiLetter(span[2])
            && char.IsAsciiLetter(span[3]);
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

    private sealed class Locals
    {
        public Locals(ILGenerator il, bool vectorize)
        {
            P = il.DeclareLocal(typeof(byte).MakeByRefType());
            Span = il.DeclareLocal(typeof(ReadOnlySpan<char>));

            UInt16Value = il.DeclareLocal(typeof(ushort));
            UInt16ValueLowerCase = il.DeclareLocal(typeof(ushort));

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
        /// Holds current character | 0x20 when processing a character at a time
        /// in binary search mode.
        /// </summary>
        public LocalBuilder UInt16ValueLowerCase { get; }

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

    private sealed class Labels
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

    [RequiresDynamicCode("ILEmitTrieFactory uses runtime IL generation.")]
    private sealed class Methods
    {
        // Caching because the methods won't change, if we're being called once we're likely to
        // be called again.
        public static readonly Methods Instance = new Methods();

        private Methods()
        {
            Add = typeof(Unsafe).GetMethod(
                nameof(Unsafe.Add),
                genericParameterCount: 1,
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { Type.MakeGenericMethodParameter(0).MakeByRefType(), typeof(int), },
                modifiers: null)
                ?.MakeGenericMethod(typeof(byte));

            if (Add is null)
            {
                throw new InvalidOperationException("Failed to find Unsafe.Add{T}(ref T, int)");
            }

            As = typeof(Unsafe).GetMethod(
               nameof(Unsafe.As),
               genericParameterCount: 2,
               BindingFlags.Public | BindingFlags.Static,
               binder: null,
               types: new[] { Type.MakeGenericMethodParameter(0).MakeByRefType(), },
               modifiers: null)
               ?.MakeGenericMethod(typeof(char), typeof(byte));

            if (As is null)
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

            GetReference = typeof(MemoryMarshal).GetMethod(
                nameof(MemoryMarshal.GetReference),
                genericParameterCount: 1,
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), },
                modifiers: null)
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
        /// <see cref="Unsafe.ReadUnaligned{T}(ref readonly byte)"/> - ReadUnaligned[ulong]
        /// </summary>
        public MethodInfo ReadUnalignedUInt64 { get; }

        /// <summary>
        /// <see cref="Unsafe.ReadUnaligned{T}(ref readonly byte)"/> - ReadUnaligned[ushort]
        /// </summary>
        public MethodInfo ReadUnalignedUInt16 { get; }
    }
}
