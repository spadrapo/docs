async function sandboxConstructor(el: HTMLElement, app: any): Promise<Sandbox> {
    //Initialize
    let instance: Sandbox = new Sandbox(el, app);
    await instance.Initalize();
    return (instance);
}

class Sandbox {
    //Field
    private _el: HTMLElement = null;
    private _app: any;
    private _sector: string = null;
    //Properties
    //Constructors
    constructor(el: HTMLElement, app: any) {
        this._el = el;
        this._app = app;
    }

    public async Initalize(): Promise<void> {
        this._sector = this._app._document.GetSector(this._el);
        const elContent: HTMLDivElement = this.GetElementContent();
        const content: string = $(elContent).html();
        await this._app._functionHandler.ResolveFunctionWithoutContext(this._sector, 'UpdateData(html,' + content +')');
        $(elContent).remove();
    }

    private GetElementContent(): HTMLDivElement {
        return (<HTMLDivElement>this._el.children[2]);
    }
}

