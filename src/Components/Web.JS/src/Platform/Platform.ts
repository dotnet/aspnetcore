import { WebAssemblyResourceLoader } from './WebAssemblyResourceLoader';

export interface Platform {
  start(resourceLoader: WebAssemblyResourceLoader): Promise<void>;

  callEntryPoint(assemblyName: string): void;

  toUint8Array(array: System_Array<any>): Uint8Array;

  getArrayLength(array: System_Array<any>): number;
  getArrayEntryPtr<TPtr extends Pointer>(array: System_Array<TPtr>, index: number, itemSize: number): TPtr;

  getObjectFieldsBaseAddress(referenceTypedObject: System_Object): Pointer;
  readInt16Field(baseAddress: Pointer, fieldOffset?: number): number;
  readInt32Field(baseAddress: Pointer, fieldOffset?: number): number;
  readUint64Field(baseAddress: Pointer, fieldOffset?: number): number;
  readFloatField(baseAddress: Pointer, fieldOffset?: number): number;
  readObjectField<T extends System_Object>(baseAddress: Pointer, fieldOffset?: number): T;
  readStringField(baseAddress: Pointer, fieldOffset?: number): string | null;
  readStructField<T extends Pointer>(baseAddress: Pointer, fieldOffset?: number): T;
}
