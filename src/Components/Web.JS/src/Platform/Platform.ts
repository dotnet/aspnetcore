// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable @typescript-eslint/ban-types */
/* eslint-disable @typescript-eslint/no-unused-vars */
import { WebAssemblyStartOptions } from './WebAssemblyStartOptions';
import { MonoConfig } from '@microsoft/dotnet-runtime';

export interface Platform {
  load(options: Partial<WebAssemblyStartOptions>, onConfigLoaded?: (loadedConfig: MonoConfig) => void): Promise<void>;
  start(): Promise<PlatformApi>;

  callEntryPoint(): Promise<unknown>;

  getArrayEntryPtr<TPtr extends Pointer>(array: System_Array<TPtr>, index: number, itemSize: number): TPtr;

  getObjectFieldsBaseAddress(referenceTypedObject: System_Object): Pointer;
  readInt16Field(baseAddress: Pointer, fieldOffset?: number): number;
  readInt32Field(baseAddress: Pointer, fieldOffset?: number): number;
  readUint64Field(baseAddress: Pointer, fieldOffset?: number): number;
  readObjectField<T extends System_Object>(baseAddress: Pointer, fieldOffset?: number): T;
  readStringField(baseAddress: Pointer, fieldOffset?: number): string | null;
  readStructField<T extends Pointer>(baseAddress: Pointer, fieldOffset?: number): T;

  beginHeapLock(): HeapLock;
  invokeWhenHeapUnlocked(callback: Function): void;
}

export type PlatformApi = {
  invokeLibraryInitializers(functionName: string, args: unknown[]): Promise<void>;
}

export interface HeapLock {
  release();
}

// We don't actually instantiate any of these at runtime. For perf it's preferable to
// use the original 'number' instances without any boxing. The definitions are just
// for compile-time checking, since TypeScript doesn't support nominal types.
export interface Pointer { Pointer__DO_NOT_IMPLEMENT: unknown }
export interface System_Object { System_Object__DO_NOT_IMPLEMENT: unknown }
export interface System_Array<T> extends System_Object { System_Array__DO_NOT_IMPLEMENT: unknown; length: number; }
