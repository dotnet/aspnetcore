// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// can be removed once userAgentData is part of lib.dom.d.ts
declare interface MonoNavigatorUserAgent extends Navigator {
    readonly userAgentData: MonoUserAgentData;
}

declare interface MonoUserAgentData {
    readonly brands: ReadonlyArray<MonoUserAgentDataBrandVersion>;
    readonly platform: string;
}

declare interface MonoUserAgentDataBrandVersion {
    brand?: string;
    version?: string;
}
