export interface ReconnectStateChangedEvent {
  state: "show" | "hide" | "retrying" | "failed" | "rejected";
  currentAttempt?: number;
  secondsToNextAttempt?: number;
}
