type SetProgressFunction = (resourcesTotal: number, resourcesLoaded: number) => void;

export class WebAssemblyProgressService {
    private static resourcesTotal: number = 0; // Maybe we need to change the initial variable or put it in the constructor, so this executes first.
    private static resourcesLoaded: number = 0;
    private static observerList = new Set<SetProgressFunction>();

    private static notifyAllObservers() {
        this.observerList.forEach(sp => sp(this.resourcesTotal, this.resourcesLoaded));
    }

    static setTotalResources(resourcesTotal: number) {
        this.resourcesTotal += resourcesTotal;
        this.notifyAllObservers();
    }

    static resourceLoaded() {
        this.resourcesLoaded++;
        this.notifyAllObservers();
    }

    static attach(observer: SetProgressFunction): void {
        this.observerList.add(observer);
    }
}
