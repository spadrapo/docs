async function sampleConstructor(el: HTMLElement, app: any): Promise<Sample> {
    //Initialize
    let instance: Sample = new Sample(el, app);
    await instance.Initalize();
    return (instance);
}

class Sample {
    //Field
    private _el: HTMLElement = null;
    private _app: any;
    //Properties
    //Constructors
    constructor(el: HTMLElement, app: any) {
        this._el = el;
        this._app = app;
    }

    public async Initalize(): Promise<void> {
        const elContent: HTMLDivElement = this.GetElementContent();
        const elCode: HTMLPreElement = this.GetElementCode();
        const content: string = $(elContent).html();
        const contentEncoded: string = $('<textarea/>').text(content).html();
        $(elCode).html(contentEncoded);
    }

    private GetElementCode(): HTMLPreElement {
        return (<HTMLPreElement>this._el.children[1]);
    }

    private GetElementContent(): HTMLDivElement {
        return (<HTMLDivElement>this._el.children[3]);
    }
}

