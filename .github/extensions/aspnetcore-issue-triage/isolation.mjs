const DEFAULT_CLEANUP_TIMEOUT_MS = 5_000;

export async function abortIsolatedSession(
  isolatedSession,
  timeoutMs = DEFAULT_CLEANUP_TIMEOUT_MS,
) {
  if (!isolatedSession) {
    return;
  }
  try {
    await withDeadline(
      () => isolatedSession.abort(),
      timeoutMs,
      "Isolated investigation abort",
    );
  } catch {
    // Bounded cleanup continues through disconnect and force-stop.
  }
}

export async function cleanupIsolatedResources(
  isolatedSession,
  client,
  timeoutMs = DEFAULT_CLEANUP_TIMEOUT_MS,
) {
  if (isolatedSession) {
    try {
      await withDeadline(
        () => isolatedSession.disconnect(),
        timeoutMs,
        "Isolated session disconnect",
      );
    } catch {
      // Stopping the client remains the authoritative cleanup path.
    }
  }

  try {
    await withDeadline(() => client.stop(), timeoutMs, "Isolated client stop");
    return;
  } catch (stopError) {
    try {
      await withDeadline(
        () => client.forceStop(),
        timeoutMs,
        "Isolated client force-stop",
      );
    } catch (forceStopError) {
      const error = new Error(
        `Unable to stop the isolated investigation client: ${forceStopError.message}`,
        { cause: stopError },
      );
      error.code = "investigation_cleanup_failed";
      throw error;
    }
  }
}

export async function withDeadline(operation, timeoutMs, description) {
  if (typeof operation !== "function" || !Number.isInteger(timeoutMs) || timeoutMs < 1) {
    throw new Error("A cleanup operation and positive timeout are required.");
  }
  let timer;
  try {
    return await Promise.race([
      Promise.resolve().then(operation),
      new Promise((_, reject) => {
        timer = setTimeout(() => {
          const error = new Error(`${description} timed out after ${timeoutMs}ms.`);
          error.code = "investigation_cleanup_timeout";
          reject(error);
        }, timeoutMs);
      }),
    ]);
  } finally {
    clearTimeout(timer);
  }
}
