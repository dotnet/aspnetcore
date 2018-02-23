import { registerFunction } from '../Interop/RegisteredFunction';
import { platform } from '../Environment';
import { MethodHandle } from '../Platform/Platform';
const httpClientAssembly = 'Microsoft.AspNetCore.Blazor.Browser';
const httpClientNamespace = `${httpClientAssembly}.Services.Temporary`;
const httpClientTypeName = 'HttpClient';
const httpClientFullTypeName = `${httpClientNamespace}.${httpClientTypeName}`;
let receiveResponseMethod: MethodHandle;

registerFunction(`${httpClientFullTypeName}.Send`, (id: number, requestUri: string) => {
  sendAsync(id, requestUri);
});

async function sendAsync(id: number, requestUri: string) {
  try {
    const response = await fetch(requestUri);
    const responseText = await response.text();
    dispatchResponse(id, response.status, responseText, null);
  } catch (ex) {
    dispatchResponse(id, 0, null, ex.toString());
  }
}

function dispatchResponse(id: number, statusCode: number, responseText: string | null, errorInfo: string | null) {
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
    platform.toDotNetString(statusCode.toString()),
    responseText === null ? null : platform.toDotNetString(responseText),
    errorInfo === null ? null : platform.toDotNetString(errorInfo.toString())
  ]);
}
