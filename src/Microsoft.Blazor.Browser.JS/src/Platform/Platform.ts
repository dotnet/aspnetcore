export interface Platform {
  start(loadAssemblyUrls: string[]): Promise<void>;

  callEntryPoint(assemblyName: string, args: System_Object[]);
  findMethod(assemblyName: string, namespace: string, className: string, methodName: string): MethodHandle;
  callMethod(method: MethodHandle, target: System_Object, args: System_Object[]): System_Object;

  toJavaScriptString(dotNetString: System_String): string;
  toDotNetString(javaScriptString: string): System_String;

  getArrayLength(array: System_Array): number;
  getArrayEntryPtr(array: System_Array, index: number, itemSize: number): Pointer;

  getHeapObjectFieldsPtr(heapObject: System_Object): Pointer;

  readHeapInt32(address: Pointer, offset?: number): number;
  readHeapObject(address: Pointer, offset?: number): System_Object;
}

// We don't actually instantiate any of these at runtime. For perf it's preferable to
// use the original 'number' instances without any boxing. The definitions are just
// for compile-time checking, since TypeScript doesn't support nominal types.
export interface MethodHandle { MethodHandle__DO_NOT_IMPLEMENT: any };
export interface System_Object { System_Object__DO_NOT_IMPLEMENT: any };
export interface System_String extends System_Object { System_String__DO_NOT_IMPLEMENT: any }
export interface System_Array extends System_Object { System_Array__DO_NOT_IMPLEMENT: any }
export interface Pointer { Pointer__DO_NOT_IMPLEMENT: any }
