declare function sampleConstructor(el: HTMLElement, app: any): Promise<Sample>;
declare class Sample {
    private _el;
    private _app;
    private _sector;
    constructor(el: HTMLElement, app: any);
    Initalize(): Promise<void>;
    private GetElementContent;
}
