# CSharpFL

CSharpFL is a teaching-oriented playground for spectrum-based fault localization (SBFL).
It contains a deliberately buggy math library, Roslyn-based source rewriters that inject
coverage logging, and ranking strategies that highlight suspicious statements by combining
coverage data with pass/fail outcomes from automated tests.

## Architecture overview
The repository centers on a staged spectrum-based fault-localization pipeline driven by the
`MathApp` console project:

1. **Target discovery & orchestration** – `Program.cs` seeds the workflow. It enumerates the
   test files and fully qualified test targets to instrument, then passes that inventory to the
   helper routine `SetInjection`. The orchestrator is responsible for sequencing instrumentation,
   invoking the test runner, and collecting coverage artifacts for every target.
2. **Instrumentation** – `SetInjection` defers to the Roslyn-based helpers in `Spectrum.cs`.
   `SpectrumMethod`, `SpectrumAll`, and `SpectrumProject` reuse the `CoverageInjector` visitor to
   rewrite syntax trees so that every statement appends a GUID identifier to a coverage log when
   executed. The `LogStatementRewriter` ensures re-running instrumentation refreshes the emitted
   GUIDs, preventing stale identifiers from surviving across runs.
3. **Execution & trace capture** – After instrumentation, `Program.RunTest` drives `dotnet test`
   with a fully qualified name filter so only the desired test executes. Each run writes GUID
   traces both to the build output directory and alongside the test sources (for example,
   `MathApp/bin/Debug/net8.0/AdditionTests.AdditionTest.coverage`).
4. **Ranking** – `Rank.cs` loads every `<TestClass>.<TestMethod>.coverage` file collected in the
   current session. It builds the pass/fail vector supplied by `Program.cs`, merges the trace data
   into a per-statement histogram, and evaluates Tarantula, Ochiai, D*, Op2, and Jaccard scores to
   surface the most suspicious statements.

Each stage feeds structured inputs into the next. You can experiment with different
instrumentation strategies or ranking formulas independently without rewriting the remainder of
the pipeline.

## Repository layout
- **MathApp/** – Console driver and core SBFL components.
  - `Program.cs` owns orchestration: it selects target tests, applies instrumentation through
    `Spectrum`, runs the filtered test executions, and prepares dictionaries that `Rank` consumes.
  - `MathOperations.cs` is the intentionally faulty math library used as the subject program.
  - `Spectrum.cs` plus `CoverageInjector` and `LogStatementRewriter` implement Roslyn-based source
    rewriting for both project files and test assets.
  - `Rank.cs` aggregates coverage files, partitions them by pass/fail status, and computes the
    Tarantula, Ochiai, D*, Op2, and Jaccard suspiciousness scores.
- **MathApp.Tests/** – xUnit test project whose outcomes drive the SBFL formulas.
- **SBFLApp/** – Scratch space for experimenting with alternate entry points; currently contains
  only build output and is excluded from the solution.
- **MathApp.sln** – Solution file wiring the console app and tests together.
- `README.md` – This guide.

> **Note:** `Program.cs` ships with Windows-style absolute paths (`D:/CS4850/...`) for the
> sample test files. Update these paths to match your local checkout or extend the bootstrap
> logic to discover the files dynamically before running the tool.

## Prerequisites
- [.NET SDK 8.0](https://dotnet.microsoft.com/download) or later.
- Windows, macOS, or Linux environment capable of running the .NET CLI.
- (Optional) IDE support for Roslyn analyzers if you want to explore the syntax tree rewriting
  code.

## Restoring and building
```bash
cd CSharpFL
dotnet restore
dotnet build
```

## Running the instrumentation pipeline
1. Ensure the absolute paths in `Program.cs` reference the test files in your workspace.
2. Run the console driver:
   ```bash
   dotnet run --project MathApp/MathApp.csproj
   ```
3. Each test execution writes `<TestClass>.<TestMethod>.coverage` files in both the build output
   directory (for example, `MathApp/bin/Debug/net8.0/`) and beside the instrumented sources. The
   files contain the GUID identifiers of every statement executed during the run.
4. The aggregated coverage dictionary is passed to `Rank`, which prints the suspiciousness scores
   for each metric.

## Running the test suite directly
You can execute the unit tests without instrumentation:
```bash
dotnet test
```

## Extending the sample
- Add new methods (and bugs!) to `MathOperations` plus matching tests to observe how the
  rankings shift across formulas.
- Experiment with alternative ranking strategies by extending `Rank.cs` with additional
  calculations or by adjusting how pass/fail data is aggregated.
- Extend `Spectrum.cs` to emit richer trace data, broaden the instrumentation surface, or detect
  target tests dynamically.

## Troubleshooting
- **No coverage files created:** Confirm that the absolute paths in `Program.cs` point to the
  expected test files and that the process has write permissions to the output directory.
- **`dotnet` command not found:** Install the .NET SDK as described in the prerequisites
  section.
- **Instrumentation repeats logs:** If you rerun instrumentation on the same file, the
  rewriters replace existing `System.IO.File.AppendAllText` calls with fresh GUID emitters so
  you can iterate without manually cleaning the test files.
