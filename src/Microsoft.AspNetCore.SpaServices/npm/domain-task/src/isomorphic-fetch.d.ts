declare module 'isomorphic-fetch' {
    var fetch: (url: string | Request, init?: RequestInit) => Promise<any>;
    export default fetch;
}
