export function requireNewCopy(moduleNameOrPath: string): any {
    // Store a reference to whatever's in the 'require' cache,
    // so we don't permanently destroy it, and then ensure there's
    // no cache entry for this module
    const resolvedModule = require.resolve(moduleNameOrPath);
    const wasCached = resolvedModule in require.cache;
    let cachedInstance;
    if (wasCached) {
        cachedInstance = require.cache[resolvedModule];
        delete require.cache[resolvedModule];
    }

    try {
        // Return a new copy
        return require(resolvedModule);
    } finally {
        // Restore the cached entry, if any
        if (wasCached) {
            require.cache[resolvedModule] = cachedInstance;
        }
    }
}
