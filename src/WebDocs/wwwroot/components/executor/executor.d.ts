declare function executorConstructor(el: HTMLElement, app: any): Promise<Executor>;
declare class Executor {
    private _el;
    private _app;
    private _sector;
    constructor(el: HTMLElement, app: any);
    Initalize(): Promise<void>;
    Increment(value: string): Promise<string>;
}
