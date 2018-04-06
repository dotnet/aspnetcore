export class EventForDotNet<TData extends UIEventArgs> {
  constructor(public readonly type: EventArgsType, public readonly data: TData) {
  }

  static fromDOMEvent(event: Event): EventForDotNet<UIEventArgs> {
    const element = event.target as Element;
    switch (event.type) {
      case 'click':
      case 'mousedown':
      case 'mouseup':
        return new EventForDotNet<UIMouseEventArgs>('mouse', { Type: event.type });
      case 'change': {
        const targetIsCheckbox = isCheckbox(element);
        const newValue = targetIsCheckbox ? !!element['checked'] : element['value'];
        return new EventForDotNet<UIChangeEventArgs>('change', { Type: event.type, Value: newValue });
      }
      case 'keypress':
        return new EventForDotNet<UIKeyboardEventArgs>('keyboard', { Type: event.type, Key: (event as any).key });
      default:
        return new EventForDotNet<UIEventArgs>('unknown', { Type: event.type });
    }
  }
}

function isCheckbox(element: Element | null) {
  return element && element.tagName === 'INPUT' && element.getAttribute('type') === 'checkbox';
}

// The following interfaces must be kept in sync with the UIEventArgs C# classes

type EventArgsType = 'mouse' | 'keyboard' | 'change' | 'unknown';

export interface UIEventArgs {
  Type: string;
}

interface UIMouseEventArgs extends UIEventArgs {
}

interface UIKeyboardEventArgs extends UIEventArgs {
  Key: string;
}

interface UIChangeEventArgs extends UIEventArgs {
  Value: string | boolean;
}
