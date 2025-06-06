export interface ReconnectStateChangedEvent {
  state: "show" | "hide" | "retrying" | "failed" | "paused" | "rejected";
  currentAttempt?: number;
  secondsToNextAttempt?: number;
  remote?: boolean;
}
