// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Shared types used by union schema tests. Mirrors the minimal-API union shapes already
// covered in the RequestDelegateGenerator tests so we exercise representative case shapes:
// primitive-paired and object-cased unions, plus a container record that references a union
// to validate component schema reuse.

internal record Kitten(string Name, int Lives);

internal record Puppy(string Name, string Breed);

internal union UnionPet(Kitten, Puppy);

internal union UnionIntString(int, string);

internal record Clinic(string Address, UnionPet Patient);
