// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;

namespace Http;

#nullable  enable

public union UnionIntString(int, string);

public union NullableCaseUnion(int?, string);

public record Cat(string Name);
public record Dog(string Breed);
public union Pet(Cat, Dog);