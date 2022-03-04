declare module 'domain' {
    var active: Domain;
}

declare module 'domain-context' {
    function get(key: string): any;
    function set(key: string, value: any): void;
    function runInNewDomain(code: () => void): void;
}
