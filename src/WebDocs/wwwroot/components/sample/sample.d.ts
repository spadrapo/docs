/// <reference types="drapo" />
declare function sampleConstructor(el: HTMLElement, app: any): Promise<Sample>;
declare class Sample {
    private _el;
    private _app;
    constructor(el: HTMLElement, app: DrapoApplication);
    Initalize(): Promise<void>;
    private GetElementCode;
    private GetElementContent;
}
