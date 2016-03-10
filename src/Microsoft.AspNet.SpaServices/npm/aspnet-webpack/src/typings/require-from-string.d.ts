declare module 'require-from-string' {
    export default function requireFromString<T>(fileContent: string): T;
}
