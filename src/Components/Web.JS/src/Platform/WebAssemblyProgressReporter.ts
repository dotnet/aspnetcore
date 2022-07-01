import { WebAssemblyProgressService } from "./WebAssemblyProgressService";

export class WebAssemblyProgressReporter {
    static setProgress(resourcesTotal: number, resourcesLoaded: number): void {
        const circle = document.querySelector('.progress') as SVGCircleElement;
        const circumference = 2 * Math.PI * circle.r.baseVal.value;
        const ring = circumference - resourcesLoaded / resourcesTotal * circumference;
        circle.style.strokeDasharray = circumference.toString() + " " + circumference.toString();
        circle.style.strokeDashoffset = ring + '';
        circle.style.display = 'block';
        const element = document.getElementById('percentage') as SVGTextElement | null;
        const percentage = Math.floor(resourcesLoaded / resourcesTotal * 100);
        if (element != null)
            element.textContent = percentage.toString() + "%";
    }

    static init() {
        const progressServiceInstance = WebAssemblyProgressService.Instance;
        progressServiceInstance.attach(this.setProgress);
    }
}
