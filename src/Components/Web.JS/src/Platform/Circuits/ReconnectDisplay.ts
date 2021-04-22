export interface ReconnectDisplay {
  show(): void;
  update(currentAttempt: number): void;
  hide(): void;
  failed(): void;
  rejected(): void;
}
