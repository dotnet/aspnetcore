// -----------------------------------------------------------------------
// <copyright file="AssemblyInfo.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

// These tests can't run in parallel because they all use the same port.
[assembly: Xunit.CollectionBehaviorAttribute(Xunit.CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true)]
