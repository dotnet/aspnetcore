import { Blazor } from "../GlobalExports";

export class WebAssemblyProgressReporter {
    /**
    * Modifies CSS of the loading ring to reflect linear progress based on the number of resources.
    * @param resourcesTotal The total number of resources retrieved from MonoPlatform.ts
    * @param resourcesLoaded The current number of resources loaded retrieved from WebAssemblyResourceLoader.ts
    */
    static setProgress(resourcesTotal: number, resourcesLoaded: number): void {
        const circle = document.getElementById('blazor-default-loading-progress') as unknown as SVGCircleElement;
        const circumference = 2 * Math.PI * circle.r.baseVal.value;
        const progressPercentage = resourcesLoaded / resourcesTotal;
        const ring = (1 - progressPercentage) * circumference;
        circle.style.strokeDasharray = `${circumference} ${circumference}`;
        circle.style.strokeDashoffset = `${ring}`;
        circle.style.display = 'block';
        const element = document.getElementById('blazor-default-loading-percentage') as unknown as SVGTextElement;
        const percentage = Math.floor(resourcesLoaded / resourcesTotal * 100);
        element!.textContent = `${percentage}%`;
    }

    /**
    * Adds the setProgress function to the list of observers in WebAssemblyProgressService.ts.
    */
    static init(): void {
        Blazor.webAssemblyProgressService?.attach(this.setProgress);
    }
}
