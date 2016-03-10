export function deepClone<T>(serializableObject: T): T {
    return JSON.parse(JSON.stringify(serializableObject));
}
