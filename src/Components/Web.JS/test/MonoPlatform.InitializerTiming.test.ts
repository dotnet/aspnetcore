/**
 * Test: JS Initializers beforeStart timing fix (Issue #54358)
 *
 * These tests validate the prepareRuntimeConfig function from MonoPlatform.ts
 * to ensure that dotnet.with*() calls happen AFTER onConfigLoaded callback invokes
 * fetchAndInvokeInitializers (which runs beforeStart JS initializer callbacks).
 *
 * Evidence from dotnet/runtime source (src/mono/browser/runtime/loader/config.ts):
 *   - onConfigLoaded is explicitly `await`ed by the runtime
 *   - Comment in source: "scripts need to be loaded before onConfigLoaded because
 *     Blazor calls `beforeStart` export in onConfigLoaded"
 *   - After onConfigLoaded returns, normalizeConfig() runs again
 *   - Resource downloads begin AFTER this point
 *   - Type signature: onConfigLoaded?: (config: MonoConfig) => void | Promise<void>
 */
import { describe, test, expect, jest, beforeEach } from '@jest/globals';

// ---------- Mocks ----------
// jest.mock() calls are HOISTED before const declarations, so we must NOT reference
// any const from outer scope inside the mock factory. Instead, we get a reference
// to the mock function via require() after mocking.

jest.mock('../src/JSInitializers/JSInitializers.WebAssembly', () => ({
    fetchAndInvokeInitializers: jest.fn(),
}));

jest.mock('../src/GlobalExports', () => ({
    Blazor: { _internal: {} },
}));

jest.mock('../src/BootErrors', () => ({
    showErrorNotification: jest.fn(),
}));

jest.mock('../src/Platform/Mono/MonoDebugger', () => ({
    attachDebuggerHotkey: jest.fn(),
}));

// Import modules after mocks
import { prepareRuntimeConfig } from '../src/Platform/Mono/MonoPlatform';
import { fetchAndInvokeInitializers } from '../src/JSInitializers/JSInitializers.WebAssembly';

// Cast to jest mock for type safety
const mockFetchAndInvokeInitializers = fetchAndInvokeInitializers as any;

// ---------- Helpers ----------

function createMockDotnetBuilder() {
    const callOrder: string[] = [];
    return {
        callOrder,
        builder: {
            withApplicationCulture: jest.fn(() => { callOrder.push('withApplicationCulture'); }),
            withApplicationEnvironment: jest.fn(() => { callOrder.push('withApplicationEnvironment'); }),
            withResourceLoader: jest.fn(() => { callOrder.push('withResourceLoader'); }),
        },
    };
}

function createMockMonoConfig(overrides: Record<string, any> = {}) {
    return {
        environmentVariables: {},
        applicationEnvironment: 'Production',
        applicationCulture: 'en-US',
        resources: {},
        ...overrides,
    };
}

// ---------- Tests ----------

describe('prepareRuntimeConfig — onConfigLoaded timing (Issue #54358)', () => {
    beforeEach(() => {
        mockFetchAndInvokeInitializers.mockReset();
    });

    test('dotnet.withResourceLoader is called AFTER fetchAndInvokeInitializers (beforeStart)', async () => {
        const callOrder: string[] = [];
        const customLoader = jest.fn();
        const options: Record<string, any> = {};

        const { builder } = createMockDotnetBuilder();
        builder.withResourceLoader = jest.fn((loader: any) => {
            callOrder.push('withResourceLoader');
            expect(loader).toBe(customLoader);
        }) as any;

        // Simulate beforeStart setting loadBootResource on the options object
        mockFetchAndInvokeInitializers.mockImplementation(async (opts: any) => {
            callOrder.push('fetchAndInvokeInitializers');
            opts.loadBootResource = customLoader;
            opts.environment = 'Development123';
            return {} as any;
        });

        const moduleConfig = prepareRuntimeConfig(options as any, builder as any);
        await moduleConfig.onConfigLoaded!(createMockMonoConfig() as any);

        // Core invariant: beforeStart runs BEFORE dotnet.with*()
        expect(callOrder.indexOf('fetchAndInvokeInitializers'))
            .toBeLessThan(callOrder.indexOf('withResourceLoader'));
        expect(builder.withResourceLoader).toHaveBeenCalledWith(customLoader);
    });

    test('dotnet.withApplicationEnvironment is called AFTER beforeStart sets environment', async () => {
        const options: Record<string, any> = {};
        const { builder, callOrder } = createMockDotnetBuilder();

        mockFetchAndInvokeInitializers.mockImplementation(async (opts: any) => {
            callOrder.push('fetchAndInvokeInitializers');
            opts.environment = 'Staging';
            return {} as any;
        });

        const moduleConfig = prepareRuntimeConfig(options as any, builder as any);
        await moduleConfig.onConfigLoaded!(createMockMonoConfig() as any);

        expect(callOrder.indexOf('fetchAndInvokeInitializers'))
            .toBeLessThan(callOrder.indexOf('withApplicationEnvironment'));
        expect(builder.withApplicationEnvironment).toHaveBeenCalledWith('Staging');
    });

    test('dotnet.with*() is NOT called when beforeStart does not set those options', async () => {
        const options: Record<string, any> = {};
        const { builder } = createMockDotnetBuilder();

        mockFetchAndInvokeInitializers.mockResolvedValue({} as any);

        const moduleConfig = prepareRuntimeConfig(options as any, builder as any);
        await moduleConfig.onConfigLoaded!(createMockMonoConfig() as any);

        expect(builder.withResourceLoader).not.toHaveBeenCalled();
        expect(builder.withApplicationEnvironment).not.toHaveBeenCalled();
        expect(builder.withApplicationCulture).not.toHaveBeenCalled();
    });

    test('configureRuntime is called AFTER fetchAndInvokeInitializers', async () => {
        const callOrder: string[] = [];
        const customConfigureRuntime = jest.fn(() => { callOrder.push('configureRuntime'); });
        const options: Record<string, any> = {};
        const { builder } = createMockDotnetBuilder();

        mockFetchAndInvokeInitializers.mockImplementation(async (opts: any) => {
            callOrder.push('fetchAndInvokeInitializers');
            opts.configureRuntime = customConfigureRuntime;
            return {} as any;
        });

        const moduleConfig = prepareRuntimeConfig(options as any, builder as any);
        await moduleConfig.onConfigLoaded!(createMockMonoConfig() as any);

        expect(callOrder.indexOf('fetchAndInvokeInitializers'))
            .toBeLessThan(callOrder.indexOf('configureRuntime'));
        expect(customConfigureRuntime).toHaveBeenCalledWith(builder);
    });

    test('onConfigLoadedCallback fires before fetchAndInvokeInitializers', async () => {
        const callOrder: string[] = [];
        const options: Record<string, any> = {};
        const { builder } = createMockDotnetBuilder();

        mockFetchAndInvokeInitializers.mockImplementation(async () => {
            callOrder.push('fetchAndInvokeInitializers');
            return {} as any;
        });

        const onConfigLoadedCallback = jest.fn(() => {
            callOrder.push('onConfigLoadedCallback');
        });

        const moduleConfig = prepareRuntimeConfig(options as any, builder as any, onConfigLoadedCallback as any);
        await moduleConfig.onConfigLoaded!(createMockMonoConfig() as any);

        expect(callOrder.indexOf('onConfigLoadedCallback'))
            .toBeLessThan(callOrder.indexOf('fetchAndInvokeInitializers'));
    });

    test('pre-configured options (via Blazor.start) are also applied in onConfigLoaded', async () => {
        const customLoader = jest.fn();
        const options: Record<string, any> = {
            loadBootResource: customLoader,
            environment: 'Production',
            applicationCulture: 'tr-TR',
        };
        const { builder } = createMockDotnetBuilder();

        mockFetchAndInvokeInitializers.mockResolvedValue({} as any);

        const moduleConfig = prepareRuntimeConfig(options as any, builder as any);
        await moduleConfig.onConfigLoaded!(createMockMonoConfig() as any);

        expect(builder.withResourceLoader).toHaveBeenCalledWith(customLoader);
        expect(builder.withApplicationEnvironment).toHaveBeenCalledWith('Production');
        expect(builder.withApplicationCulture).toHaveBeenCalledWith('tr-TR');
    });
});
