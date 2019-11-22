import { platform } from '../Environment';
import { MethodHandle, System_String, System_Array } from '../Platform/Platform';
const httpClientAssembly = 'Microsoft.AspNetCore.Blazor';
const httpClientNamespace = `${httpClientAssembly}.Http`;
const httpClientTypeName = 'WebAssemblyHttpMessageHandler';
let receiveResponseMethod: MethodHandle;
let allocateArrayMethod: MethodHandle;

// These are the functions we're making available for invocation from .NET
export const internalFunctions = {
  sendAsync,
};

async function sendAsync(id: number, body: System_Array<any>, jsonFetchArgs: System_String) {
  let response: Response;
  let responseData: ArrayBuffer;

  const fetchOptions: FetchOptions = JSON.parse(platform.toJavaScriptString(jsonFetchArgs));
  const requestInit: RequestInit = Object.assign(convertToRequestInit(fetchOptions.requestInit), fetchOptions.requestInitOverrides);

  if (body) {
    requestInit.body = platform.toUint8Array(body);
  }

  try {
    response = await fetch(fetchOptions.requestUri, requestInit);
    responseData = await response.arrayBuffer();
  } catch (ex) {
    dispatchErrorResponse(id, ex.toString());
    return;
  }

  dispatchSuccessResponse(id, response, responseData);
}

function convertToRequestInit(blazorRequestInit: BlazorRequestInit) {
  return {
    credentials: blazorRequestInit.credentials,
    method: blazorRequestInit.method,
    headers: blazorRequestInit.headers.map(item => [item.name, item.value])
  };
}

function dispatchSuccessResponse(id: number, response: Response, responseData: ArrayBuffer) {
  const responseDescriptor: ResponseDescriptor = {
    statusCode: response.status,
    statusText: response.statusText,
    headers: [],
  };
  response.headers.forEach((value, name) => {
    responseDescriptor.headers.push({ name: name, value: value });
  });

  if (!allocateArrayMethod) {
    allocateArrayMethod = platform.findMethod(
      httpClientAssembly,
      httpClientNamespace,
      httpClientTypeName,
      'AllocateArray'
    );
  }

  // allocate a managed byte[] of the right size
  const dotNetArray = platform.callMethod(allocateArrayMethod, null, [platform.toDotNetString(responseData.byteLength.toString())]) as System_Array<any>;

  // get an Uint8Array view of it
  const array = platform.toUint8Array(dotNetArray);

  // copy the responseData to our managed byte[]
  array.set(new Uint8Array(responseData));

  dispatchResponse(
    id,
    platform.toDotNetString(JSON.stringify(responseDescriptor)),
    dotNetArray,
    /* errorMessage */ null
  );
}

function dispatchErrorResponse(id: number, errorMessage: string) {
  dispatchResponse(
    id,
    /* responseDescriptor */ null,
    /* responseText */ null,
    platform.toDotNetString(errorMessage)
  );
}

function dispatchResponse(id: number, responseDescriptor: System_String | null, responseData: System_Array<any> | null, errorMessage: System_String | null) {
  if (!receiveResponseMethod) {
    receiveResponseMethod = platform.findMethod(
      httpClientAssembly,
      httpClientNamespace,
      httpClientTypeName,
      'ReceiveResponse'
    );
  }

  platform.callMethod(receiveResponseMethod, null, [
    platform.toDotNetString(id.toString()),
    responseDescriptor,
    responseData,
    errorMessage,
  ]);
}

// Keep these in sync with the .NET equivalent in WebAssemblyHttpMessageHandler.cs
interface FetchOptions {
  requestUri: string;
  requestInit: BlazorRequestInit;
  requestInitOverrides: RequestInit;
}

interface BlazorRequestInit {
  credentials: string;
  headers: Header[];
  method: string;
}

interface ResponseDescriptor {
  // We don't have BodyText in here because if we did, then in the JSON-response case (which
  // is the most common case), we'd be double-encoding it, since the entire ResponseDescriptor
  // also gets JSON encoded. It would work but is twice the amount of string processing.
  statusCode: number;
  statusText: string;
  headers: Header[];
}

interface Header {
  name: string;
  value: string;
}
