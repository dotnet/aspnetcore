// Runs `npm ci` with retries to mitigate transient network failures.
//
// npm's built-in fetch retry (configured via fetch-retries in .npmrc) does not
// reliably recover from errors that occur while reading a response body, such as
// "Invalid response body while trying to fetch ... read ECONNRESET". Those
// failures surface as a non-zero exit from `npm ci` and would otherwise fail the
// build non-deterministically. Wrapping the command in a retry loop makes the
// restore step robust against these transient registry/network hiccups.
import { execSync } from 'child_process';

const maxAttempts = 5;
const baseDelayMs = 15000;

function sleep(ms) {
  // Synchronous sleep so we can pause between attempts without async plumbing.
  Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, ms);
}

for (let attempt = 1; attempt <= maxAttempts; attempt++) {
  try {
    console.log(`Running 'npm ci' (attempt ${attempt}/${maxAttempts})...`);
    execSync('npm ci', { stdio: 'inherit' });
    process.exit(0);
  } catch (error) {
    console.error(`'npm ci' failed on attempt ${attempt}/${maxAttempts}: ${error.message}`);

    if (attempt === maxAttempts) {
      console.error(`'npm ci' failed after ${maxAttempts} attempts.`);
      process.exit(1);
    }

    const delayMs = baseDelayMs * attempt;
    console.log(`Retrying 'npm ci' in ${delayMs / 1000} seconds...`);
    sleep(delayMs);
  }
}
