export interface Platform {
  start(loadAssemblyUrls: string[]): Promise<void>;

  callEntryPoint(assemblyName: string, entrypointMethod: string, args: (System_Object | null)[]);
  findMethod(assemblyName: string, namespace: string, className: string, methodName: string): MethodHandle;
  callMethod(method: MethodHandle, target: System_Object | null, args: (System_Object | null)[]): System_Object;

  toJavaScriptString(dotNetString: System_String): string;
  toDotNetString(javaScriptString: string): System_String;

  toUint8Array(array: System_Array<any>): Uint8Array;

  getArrayLength(array: System_Array<any>): number;
  getArrayEntryPtr<TPtr extends Pointer>(array: System_Array<TPtr>, index: number, itemSize: number): TPtr;

  getObjectFieldsBaseAddress(referenceTypedObject: System_Object): Pointer;
  readInt32Field(baseAddress: Pointer, fieldOffset?: number): number;
  readFloatField(baseAddress: Pointer, fieldOffset?: number): number;
  readObjectField<T extends System_Object>(baseAddress: Pointer, fieldOffset?: number): T;
  readStringField(baseAddress: Pointer, fieldOffset?: number): string | null;
  readStructField<T extends Pointer>(baseAddress: Pointer, fieldOffset?: number): T;
}

// We don't actually instantiate any of these at runtime. For perf it's preferable to
// use the original 'number' instances without any boxing. The definitions are just
// for compile-time checking, since TypeScript doesn't support nominal types.
export interface MethodHandle { MethodHandle__DO_NOT_IMPLEMENT: any };
export interface System_Object { System_Object__DO_NOT_IMPLEMENT: any };
export interface System_String extends System_Object { System_String__DO_NOT_IMPLEMENT: any }
export interface System_Array<T> extends System_Object { System_Array__DO_NOT_IMPLEMENT: any }
export interface Pointer { Pointer__DO_NOT_IMPLEMENT: any }
