export interface ReconnectDisplay {
  show(): void;
  hide(): void;
  failed(): void;
  rejected(): void;
}
