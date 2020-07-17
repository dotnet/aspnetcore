// Import type definitions to ensure that the global declaration
// is of BINDING is included when tests run
import './Mono/MonoTypes';
import { System_String } from './Platform';

interface TimingEntry {
    // To minimize overhead, don't even decode the strings that arrive from .NET. Assume they are compile-time constants
    // and hence the memory address will be fixed, so we can just store the pointer value.
    name: string | System_String;
    type: 'start' | 'end';
    timestamp: number;
}

interface TraceEvent {
    // https://docs.google.com/document/d/1CvAClvFfyA5R-PhYUmn5OOQtYMH4h6I0nSsKchNAySU/preview
    name: string;
    cat: string; // Category
    ph: 'B' | 'E'; // Phase
    ts: number; // Timestamp in microseconds
    pid: number; // Process ID
    tid: number; // Thread ID
}

let updateCapturingStateInHost: (isCapturing: boolean) => void;
let captureStartTime = 0;
const blazorProfilingEnabledKey = 'blazorProfilingEnabled';
const profilingEnabled = !!localStorage[blazorProfilingEnabledKey];
const entryLog: TimingEntry[] = [];
const openRegionsStack: (string | System_String)[] = [];

export function setProfilingEnabled(enabled: boolean) {
    // We only wire up the hotkeys etc. if the following localStorage entry is present during startup
    // This is to ensure we're not interfering with any other hotkeys that developers might want to
    // use for different purposes, plus it gives us a single point where we can notify .NET code during
    // startup about whether profiling should be enabled.
    localStorage[blazorProfilingEnabledKey] = (enabled !== false);
    location.reload();
}

export function initializeProfiling(setCapturingCallback: ((isCapturing: boolean) => void) | null) {
    if (!profilingEnabled) {
        return;
    }

    updateCapturingStateInHost = setCapturingCallback || (() => {});

    // Attach hotkey (alt/cmd)+shift+p
    const altKeyName = navigator.platform.match(/^Mac/i) ? 'Cmd' : 'Alt';
    console.info(`Profiling hotkey: Shift+${altKeyName}+P (when application has focus)`);
    document.addEventListener('keydown', evt => {
        if (evt.shiftKey && (evt.metaKey || evt.altKey) && evt.code === 'KeyP') {
            toggleCaptureEnabled();
        }
    });
}

export function profileStart(name: System_String | string) {
    if (!captureStartTime) {
        return;
    }

    const startTime = performance.now();
    openRegionsStack.push(name);
    entryLog.push({ name: name, type: 'start', timestamp: startTime });
}

export function profileEnd(name: System_String | string) {
    if (!captureStartTime) {
        return;
    }

    const endTime = performance.now();
    const poppedRegionName = openRegionsStack.pop();
    if (!poppedRegionName) {
        throw new Error(`Profiling mismatch: tried to end profiling for '${readJsString(name)}', but the stack was empty.`);
    } else if (poppedRegionName !== name) {
        throw new Error(`Profiling mismatch: tried to end profiling for '${readJsString(name)}', but the top stack item was '${readJsString(poppedRegionName)}'.`);
    }

    entryLog.push({ name: name, type: 'end', timestamp: endTime });
}

function profileReset() {
    openRegionsStack.length = 0;
    entryLog.length = 0;
    captureStartTime = 0;
    updateCapturingStateInHost(false);
}

function profileExport() {
    const traceEvents: TraceEvent[] = entryLog.map(entry => ({
        name: readJsString(entry.name)!,
        cat: 'PERF',
        ph: entry.type === 'start' ? 'B': 'E',
        ts: (entry.timestamp - captureStartTime) * 1000,
        pid: 0,
        tid: 0,
    }));
    const traceEventsJson = JSON.stringify(traceEvents);
    const traceEventsBuffer = new TextEncoder().encode(traceEventsJson);
    const anchorElement = document.createElement('a');
    anchorElement.href = URL.createObjectURL(new Blob([traceEventsBuffer]));
    anchorElement.setAttribute('download', 'trace.json');
    anchorElement.click();
    URL.revokeObjectURL(anchorElement.href);
}

function toggleCaptureEnabled() {
    if (!captureStartTime) {
        displayOverlayMessage('Started capturing performance profile...');
        updateCapturingStateInHost(true);
        captureStartTime = performance.now();
    } else {
        displayOverlayMessage('Finished capturing performance profile');
        profileExport();
        profileReset();
    }
}

function displayOverlayMessage(message: string) {
    const elem = document.createElement('div');
    elem.textContent = message;
    elem.setAttribute('style', 'position: absolute; z-index: 99999; font-family: \'Sans Serif\'; top: 0; left: 0; padding: 4px; font-size: 12px; background-color: purple; color: white;');
    document.body.appendChild(elem);
    setTimeout(() => document.body.removeChild(elem), 3000);
}

function readJsString(str: string | System_String) {
    // This is expensive, so don't do it while capturing timings. Only do it as part of the export process.
    return typeof str === 'string' ? str : BINDING.conv_string(str);
}

// These globals deliberately differ from our normal conventions for attaching functions inside Blazor.*
// because the intention is to minimize overhead in all reasonable ways. Having any dot-separators in the
// name would cause extra string allocations on every invocation.
window['_blazorProfileStart'] = profileStart;
window['_blazorProfileEnd'] = profileEnd;
