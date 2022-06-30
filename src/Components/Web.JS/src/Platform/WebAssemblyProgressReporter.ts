import { WebAssemblyProgressService } from "./WebAssemblyProgressService";

export class WebAssemblyProgressReporter {
    static setProgress(resourcesLoaded: number, resourcesTotal: number): void {
        // Change SVG/CSS for progress bar (WIP)
    }

    static init() {
        WebAssemblyProgressService.attach(this.setProgress);
    }
}
