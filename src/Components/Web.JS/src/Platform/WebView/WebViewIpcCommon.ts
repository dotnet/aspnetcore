const ipcMessagePrefix = '__bwv:';

export function serializeMessage(messageType: string, args: any[]): string {
  return `${ipcMessagePrefix}${JSON.stringify([messageType, ...args])}`;
}

export function tryDeserializeMessage(message: string): IpcMessage | null {
  if (!message || !message.startsWith(ipcMessagePrefix)) {
    return null;
  }

  const messageAfterPrefix = message.substring(ipcMessagePrefix.length);
  const [messageType, ...args] = JSON.parse(messageAfterPrefix);
  return { messageType, args };
}

interface IpcMessage {
  messageType: string;
  args: any[];
}
