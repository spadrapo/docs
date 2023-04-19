async function codeConstructor(el: HTMLElement, app: DrapoApplication): Promise<Code> {
    //Initialize
    let instance: Code = new Code(el, app);
    await instance.Initalize();
    return (instance);
}

class Code {
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
        const content: string = this._app.Document.GetHTML(elContent);;
        const contentEncoded: string = this._app.Document.GetHTMLEncoded(content);
        await this._app.Document.RemoveElement(elContent, false);
        this._app.Document.SetHTML(elCode, contentEncoded);
        elCode.setAttribute('d-pre', 'true');
    }

    private GetElementContent(): HTMLDivElement {
        return (<HTMLDivElement>this._el.children[0]);
    }

    private GetElementCode(): HTMLPreElement {
        return (<HTMLPreElement>this._el.children[1]);
    }
}

