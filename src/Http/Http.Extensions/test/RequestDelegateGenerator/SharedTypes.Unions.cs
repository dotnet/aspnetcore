// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;

namespace Http;

#nullable enable

// By convention every union type in this file starts with "Union".

// Simple unambiguous primitive-paired unions
public union UnionIntString(int, string);
public union UnionByteString(byte, string);
public union UnionShortString(short, string);
public union UnionLongString(long, string);
public union UnionDecimalString(decimal, string);
public union UnionDoubleString(double, string);
public union UnionBoolString(bool, string);
public union UnionGuidInt(Guid, int);          // String + Number
public union UnionDateTimeInt(DateTime, int);  // String + Number
public union UnionCharInt(char, int);          // String + Number (char serializes as JSON string)

// Nullable case
public union UnionNullableIntString(int?, string);

// Object-case union.
public record Cat(string Name);
public record Dog(string Breed);
public union UnionPet(Cat, Dog);

// Derived type that is NOT a declared case of UnionPet — used to verify STJ resolves to
// the nearest declared ancestor (Dog) when handed a SausageDog.
public record SausageDog(string Breed, double Length) : Dog(Breed);

// Nested-union scenarios.
public union UnionInner(int, string);
public union UnionOuter(UnionInner, bool); // union case is itself a union

// Ambiguous unions 
#pragma warning disable SYSLIB1227
public union UnionIntShort(int, short);            // both → Number
public union UnionDateTimeString(DateTime, string); // both → String
#pragma warning restore SYSLIB1227
