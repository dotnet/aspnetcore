declare module 'npm-which' {
    interface NpmWhichContext {
        sync(executableName: string): string;
    }

    function defaultFunction(rootDir: string) : NpmWhichContext;
    export = defaultFunction;
}
