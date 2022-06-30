type SetProgressFunction = (resourcesLoaded: number, resourcesTotal: number) => void;

export class WebAssemblyProgressService {
    private static resourcesLoaded: number = 0;
    private static resourcesTotal: number = 0;
    private static observerList = new Set<SetProgressFunction>();

    private static notifyAllObservers() {
        this.observerList.forEach(sp => sp(this.resourcesLoaded, this.resourcesTotal));
    }

    static resourceLoaded() {
        this.resourcesLoaded++;
        this.notifyAllObservers();
    }

    static setTotalResources(resourcesTotal: number) {
        this.resourcesTotal = resourcesTotal;
        this.notifyAllObservers();
    }

    static attach(observer: SetProgressFunction): void {
        this.observerList.add(observer);
    }
}
