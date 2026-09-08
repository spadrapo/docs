# Contract: `Drapo.Tooling` library public surface

**Assembly**: `Drapo.Tooling.dll` (`net8.0`). **Package references**: `Drapo`, `Newtonsoft.Json`.
**Forbidden references**: anything under `Microsoft.AspNetCore.*` (asserted by a test).

## Moved from WebDocs (behaviour unchanged, namespace renamed)

| From (`WebDocs.*`) | To (`Drapo.Tooling.*`) |
|--------------------|------------------------|
| `Models.AttributeVM` | `Models.AttributeVM` |
| `Models.FunctionVM`, `FunctionParameterVM`, `FunctionSampleVM` | `Models.*` (same names) |
| `Models.DrapoDiagnosticVM`, `DrapoValidationResultVM` | `Models.*` (same names) |
| `Services.IDrapoEngineCatalog` / `DrapoEngineCatalog` | `Services.*` |
| `Services.IFunctionService` / `FunctionService` | `Services.*` |
| `Services.IAttributeService` / `AttributeService` | `Services.*` |
| `Services.IDrapoValidatorService` / `DrapoValidatorService` | `Services.*` |
| `Helpers.DrapoDocContent` | `Helpers.DrapoDocContent` |

Property names, JSON casing and method signatures are preserved exactly.

## Added

```csharp
namespace Drapo.Tooling.Content
{
    /// Where the convention-bound documentation content lives (the folder that contains
    /// "functions/" and "menu/").
    public interface IDrapoContentRoot { string AppPath { get; } }

    public sealed class DrapoContentRoot : IDrapoContentRoot
    {
        public DrapoContentRoot(string appPath);   // throws ArgumentException if null/empty
        public string AppPath { get; }
    }
}

namespace Drapo.Tooling.Services
{
    public interface IDrapoEngineCatalog
    {
        bool IsValidFunction(string name);          // existing
        bool IsValidAttribute(string name);         // existing
        string EngineVersion { get; }               // existing
        IReadOnlyCollection<string> Functions { get; }          // NEW: lower-case, fixed names
        IReadOnlyCollection<string> Attributes { get; }         // NEW: lower-case, fixed names (no prefixes)
        IReadOnlyCollection<string> AttributePrefixes { get; }  // NEW: lower-case, each ends with '-'
    }
}

namespace Drapo.Tooling.Helpers
{
    /// Shared parsing of d-on-* handler expressions (extracted from DrapoValidatorService so the
    /// validator and the language server use one implementation).
    public static class DrapoHandlerSyntax
    {
        public sealed class FunctionCall { public string Name; public int NameIndex; public string Arguments; }
        public static IEnumerable<FunctionCall> ScanFunctionCalls(string handlerExpression);
        public static List<string> SplitArguments(string arguments);  // top-level commas only
    }
}
```

## Constructor changes

| Type | Before | After |
|------|--------|-------|
| `FunctionService` | `(IWebHostEnvironment env)` | `(IDrapoContentRoot root)` — paths become `Path.Combine(root.AppPath, "functions", …)` |
| `AttributeService` | `(IWebHostEnvironment env)` | `(IDrapoContentRoot root)` — `Path.Combine(root.AppPath, "menu")` |
| `DrapoEngineCatalog` | `()` | unchanged |
| `DrapoValidatorService` | `(IDrapoEngineCatalog, IFunctionService)` | unchanged |

## Required WebDocs changes (consumer side)

- `WebDocs.csproj`: `<ProjectReference Include="..\Drapo.Tooling\Drapo.Tooling.csproj" />`.
- `Startup.ConfigureServices`: register `IDrapoContentRoot` as a singleton pointing at
  `<wwwroot>/app` before the existing registrations (which keep their lifetimes: catalog singleton,
  others scoped).
- `using` updates in `Controllers/AttributeController.cs`, `Controllers/FunctionController.cs`,
  `Tools/{Attribute,Function,Validation}Tool.cs`, `Services/{ConceptService,DataTypeService}.cs`.
- `Dockerfile`: add `COPY ["Drapo.Tooling/Drapo.Tooling.csproj", "Drapo.Tooling/"]` before
  `dotnet restore`.
- Delete the moved files from `src/WebDocs/{Models,Services,Helpers}`.

## Behavioural guarantees (verified by tests)

1. `DrapoValidatorService.Validate(text)` returns the same diagnostics as before the move for the
   parity corpus.
2. `FunctionService.GetList()/Get()`, `AttributeService.GetList()/Get()` return identical payloads
   when pointed at `src/WebDocs/wwwroot/app`.
3. `DrapoEngineCatalog.Attributes ∪ AttributePrefixes` and `Functions` are exactly the sets used by
   `IsValidAttribute` / `IsValidFunction`.
