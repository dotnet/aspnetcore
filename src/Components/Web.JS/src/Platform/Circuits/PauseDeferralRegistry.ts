// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// What a deferral participant is scoped to. The framework only names 'server' (server-initiated
// pause); anything else runs on client pauses. `(string & {})` keeps 'server' autocompletion while
// allowing arbitrary package-defined labels.
export type PauseSource = 'server' | (string & {});
export type PauseDeferralHandler = (signal: AbortSignal, source?: PauseSource) => void | Promise<void>;

export interface PauseDeferralEntry {
  handler: PauseDeferralHandler;
  source?: PauseSource;
}

// Module-scoped registry of pause-deferral participants, so callbacks survive across circuit reconnect/resume.
const handlers = new Set<PauseDeferralEntry>();

// Registers a participant and returns an unsubscribe callback.
export function registerPauseDeferral(handler: PauseDeferralHandler, source?: PauseSource): () => void {
  const entry: PauseDeferralEntry = { handler, source };
  handlers.add(entry);
  return () => handlers.delete(entry);
}

export function getMatchingPauseDeferrals(matches: (entry: PauseDeferralEntry) => boolean): PauseDeferralEntry[] {
  return [...handlers].filter(matches);
}
