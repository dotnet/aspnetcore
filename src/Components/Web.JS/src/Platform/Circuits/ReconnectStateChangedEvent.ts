export interface ReconnectStateChangedEvent {
  state: "show" | "hide" | "retrying" | "failed" | "resume-failed" | "paused" | "rejected";
  currentAttempt?: number;
  secondsToNextAttempt?: number;
  remote?: boolean;
}
