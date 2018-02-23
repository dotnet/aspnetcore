import { registerFunction } from '../Interop/RegisteredFunction';
import { platform } from '../Environment';
import { MethodHandle, System_String } from '../Platform/Platform';
const httpClientAssembly = 'Microsoft.AspNetCore.Blazor.Browser';
const httpClientNamespace = `${httpClientAssembly}.Services.Temporary`;
const httpClientTypeName = 'HttpClient';
const httpClientFullTypeName = `${httpClientNamespace}.${httpClientTypeName}`;
let receiveResponseMethod: MethodHandle;

registerFunction(`${httpClientFullTypeName}.Send`, (id: number, method: string, requestUri: string, body: string | null, headersJson: string | null) => {
  sendAsync(id, method, requestUri, body, headersJson);
});

async function sendAsync(id: number, method: string, requestUri: string, body: string | null, headersJson: string | null) {
  let response: Response;
  let responseText: string;
  try {
    response = await fetch(requestUri, {
      method: method,
      body: body,
      headers: headersJson ? (JSON.parse(headersJson) as string[][]) : undefined
    });
    responseText = await response.text();
  } catch (ex) {
    dispatchErrorResponse(id, ex.toString());
    return;
  }

  dispatchSuccessResponse(id, response, responseText);
}

function dispatchSuccessResponse(id: number, response: Response, responseText: string) {
  const responseDescriptor: ResponseDescriptor = {
    StatusCode: response.status,
    Headers: []
  };
  response.headers.forEach((value, name) => {
    responseDescriptor.Headers.push([name, value]);
  });

  dispatchResponse(
    id,
    platform.toDotNetString(JSON.stringify(responseDescriptor)),
    platform.toDotNetString(responseText), // TODO: Consider how to handle non-string responses
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

function dispatchResponse(id: number, responseDescriptor: System_String | null, responseText: System_String | null, errorMessage: System_String | null) {
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
    responseText,
    errorMessage,
  ]);
}

// Keep this in sync with the .NET equivalent in HttpClient.cs
interface ResponseDescriptor {
  // We don't have BodyText in here because if we did, then in the JSON-response case (which
  // is the most common case), we'd be double-encoding it, since the entire ResponseDescriptor
  // also gets JSON encoded. It would work but is twice the amount of string processing.
  StatusCode: number;
  Headers: string[][];
}
