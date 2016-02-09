// TODO: Move this on to definitelytyped, and take a dependency on whatwg-fetch
// so that the 'fetch' function can have the correct type args

declare module 'domain-task' {
    function addTask(task: PromiseLike<any>): void;
}

declare module 'domain-task/fetch' {
    function fetch(url, options?): Promise<any>;
}
