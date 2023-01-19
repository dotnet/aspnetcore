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
