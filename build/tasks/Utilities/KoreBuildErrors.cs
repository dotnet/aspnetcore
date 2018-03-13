// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace RepoTasks.Utilities
{
    public static class KoreBuildErrors
    {
        public const string Prefix = "KRB";

        // Typically used in repos in Directory.Build.targets
        public const int PackagesHaveNotYetBeenPinned = 1001;

        // Warnings
        public const int DotNetAssetVersionIsFloating = 2000;
        public const int RepoVersionDoesNotMatchProjectVersion = 2001;
        public const int RepoPackageVersionDoesNotMatchProjectPackageVersion = 2002;
        public const int DuplicatePackageReference = 2003;

        // NuGet errors
        public const int InvalidNuspecFile = 4001;
        public const int PackageReferenceHasVersion = 4002;
        public const int DotNetCliReferenceReferenceHasVersion = 4003;
        public const int PackageVersionNotFoundInLineup = 4004;
        public const int UndefinedExternalDependency = 4005;
        public const int EmptyPackageReferenceVersion = 4006;

        // Other unknown errors
        public const int PolicyFailedToApply = 5000;
        public const int UnknownPolicyType = 5001;
    }
}
