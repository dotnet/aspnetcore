/*
In general, we want the Node child processes to be terminated as soon as the parent .NET processes exit,
because we have no further use for them. If the .NET process shuts down gracefully, it will run its
finalizers, one of which (in OutOfProcessNodeInstance.cs) will kill its associated Node process immediately.

But if the .NET process is terminated forcefully (e.g., on Linux/OSX with 'kill -9'), then it won't have
any opportunity to shut down its child processes, and by default they will keep running. In this case, it's
up to the child process to detect this has happened and terminate itself.

There are many possible approaches to detecting when a parent process has exited, most of which behave
differently between Windows and Linux/OS X:

 - On Windows, the parent process can mark its child as being a 'job' that should auto-terminate when
   the parent does (http://stackoverflow.com/a/4657392). Not cross-platform.
 - The child Node process can get a callback when the parent disconnects (process.on('disconnect', ...)).
   But despite http://stackoverflow.com/a/16487966, no callback fires in any case I've tested (Windows / OS X).
 - The child Node process can get a callback when its stdin/stdout are disconnected, as described at
   http://stackoverflow.com/a/15693934. This works well on OS X, but calling stdout.resume() on Windows
   causes the process to terminate prematurely.
 - I don't know why, but on Windows, it's enough to invoke process.stdin.resume(). For some reason this causes
   the child Node process to exit as soon as the parent one does, but I don't see this documented anywhere.
 - You can poll to see if the parent process, or your stdin/stdout connection to it, is gone
   - You can directly pass a parent process PID to the child, and then have the child poll to see if it's
     still running (e.g., using process.kill(pid, 0), which doesn't kill it but just tests whether it exists,
     as per https://nodejs.org/api/process.html#process_process_kill_pid_signal)
   - Or, on each poll, you can try writing to process.stdout. If the parent has died, then this will throw.
     However I don't see this documented anywhere. It would be nice if you could just poll for whether or not
     process.stdout is still connected (without actually writing to it) but I haven't found any property whose
     value changes until you actually try to write to it.

Of these, the only cross-platform approach that is actually documented as a valid strategy is simply polling
to check whether the parent PID is still running. So that's what we do here.
*/

const pollIntervalMs = 1000;

export function exitWhenParentExits(parentPid: number, ignoreSigint: boolean) {
    setInterval(() => {
        if (!processExists(parentPid)) {
            // Can't log anything at this point, because out stdout was connected to the parent,
            // but the parent is gone.
            process.exit();
        }
    }, pollIntervalMs);

    if (ignoreSigint) {
        // Pressing ctrl+c in the terminal sends a SIGINT to all processes in the foreground process tree.
        // By default, the Node process would then exit before the .NET process, because ASP.NET implements
        // a delayed shutdown to allow ongoing requests to complete.
        //
        // This is problematic, because if Node exits first, the CopyToAsync code in ConditionalProxyMiddleware
        // will experience a read fault, and logs a huge load of errors. Fortunately, since the Node process is
        // already set up to shut itself down if it detects the .NET process is terminated, all we have to do is
        // ignore the SIGINT. The Node process will then terminate automatically after the .NET process does.
        //
        // A better solution would be to have WebpackDevMiddleware listen for SIGINT and gracefully close any
        // ongoing EventSource connections before letting the Node process exit, independently of the .NET
        // process exiting. However, doing this well in general is very nontrivial (see all the discussion at
        // https://github.com/nodejs/node/issues/2642).
        process.on('SIGINT', () => {
            console.log('Received SIGINT. Waiting for .NET process to exit...');
        });
    }
}

function processExists(pid: number) {
    try {
        // Sending signal 0 - on all platforms - tests whether the process exists. As long as it doesn't
        // throw, that means it does exist.
        process.kill(pid, 0);
        return true;
    } catch (ex) {
        // If the reason for the error is that we don't have permission to ask about this process,
        // report that as a separate problem.
        if (ex.code === 'EPERM') {
            throw new Error(`Attempted to check whether process ${pid} was running, but got a permissions error.`);
        }

        return false;
    }
}
