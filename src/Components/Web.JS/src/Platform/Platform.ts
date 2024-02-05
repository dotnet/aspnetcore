// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable @typescript-eslint/ban-types */
/* eslint-disable @typescript-eslint/no-unused-vars */
import { MonoObject, MonoString, MonoArray } from 'dotnet-runtime/dotnet-legacy';
import { WebAssemblyStartOptions } from './WebAssemblyStartOptions';
import { MonoConfig } from 'dotnet-runtime';

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
  readStringField(baseAddress: Pointer, fieldOffset?: number, readBoolValueAsString?: boolean): string | null;
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
export interface MethodHandle { MethodHandle__DO_NOT_IMPLEMENT: unknown }
export type System_Object = MonoObject;
export interface System_Boolean { System_Boolean__DO_NOT_IMPLEMENT: unknown }
export interface System_Byte { System_Byte__DO_NOT_IMPLEMENT: unknown }
export interface System_Int { System_Int__DO_NOT_IMPLEMENT: unknown }
export interface System_String extends System_Object, MonoString { }
export interface System_Array<T> extends System_Object, MonoArray { }
export interface Pointer { Pointer__DO_NOT_IMPLEMENT: unknown }
