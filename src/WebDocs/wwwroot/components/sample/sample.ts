async function sampleConstructor(el: HTMLElement, app: any): Promise<Sample> {
    //Initialize
    let instance: Sample = new Sample(el, app);
    await instance.Initalize();
    return (instance);
}

class Sample {
    //Field
    private _el: HTMLElement = null;
    private _app: DrapoApplication;
    //Properties
    //Constructors
    constructor(el: HTMLElement, app: DrapoApplication) {
        this._el = el;
        this._app = app;
    }

    public async Initalize(): Promise<void> {
        const elContent: HTMLDivElement = this.GetElementContent();
        const elCode: HTMLPreElement = this.GetElementCode();
        const content: string = this._app.Document.GetHTML(elContent);
        const contentEncoded: string = this._app.Document.GetHTMLEncoded(content);
        this._app.Document.SetHTML(elCode, contentEncoded);
        elCode.setAttribute('d-pre', 'true');
    }

    private GetElementCode(): HTMLPreElement {
        return (<HTMLPreElement>this._el.children[1]);
    }

    private GetElementContent(): HTMLDivElement {
        return (<HTMLDivElement>this._el.children[3]);
    }
}

