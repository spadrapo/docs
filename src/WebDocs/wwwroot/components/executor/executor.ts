async function executorConstructor(el: HTMLElement, app: any): Promise<Executor> {
    //Initialize
    let instance: Executor = new Executor(el, app);
    await instance.Initalize();
    return (instance);
}

class Executor {
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
        this._sector = this._app.Document.GetSector(this._el);
        const sectorRegister: string = this._el.getAttribute("dc-sector");
        if (sectorRegister != null)
            await this._app.FunctionHandler.ResolveFunctionWithoutContext(this._sector, this._el, "UpdateData(" + sectorRegister + "," + this._sector + ")");
    }

    public async Increment(value : string): Promise<string> {
        let valueNumber: number = this._app.Parser.GetStringAsNumber(value) + 1;
        return (valueNumber.toString());
    }
}