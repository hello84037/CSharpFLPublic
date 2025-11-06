# CSharpFL

CSharpFL is a sample C# solution that demonstrates automated spectrum-based fault localization (SBFL). The `SBFLApp` console utility instruments the unit tests, executes them, collects coverage data, and ranks potentially faulty statements with several well-known SBFL metrics.

## Repository layout
- **MathApp/** – Minimal console application used as the subject under test. `MathOperations.cs` contains the arithmetic routines exercised by the tests, and `SeriesOperations.cs` provides richer sequence and statistics helpers for multi-file experimentation.
- **MathApp.Tests/** – xUnit test project with addition, subtraction, and series-analysis scenarios. Some subtraction tests intentionally fail so that the fault-localization workflow has both passing and failing executions to analyze.
- **SBFLApp/** – Console application that performs the SBFL workflow.
  - `Program.cs` discovers test methods, keeps instrumentation in sync, coordinates per-test execution, and manages coverage artifacts in a dedicated working folder.
  - `Spectrum.cs` and `LogStatementRewriter.cs` contain the Roslyn rewriters that add and clean up logging statements.
  - `Rank.cs` calculates suspiciousness scores using the Tarantula, Ochiai, D*, Op2, and Jaccard formulas and exports the values to CSV.
- **MathApp.sln** – Solution file tying the application and test project together.

## Prerequisites
- [.NET SDK 8.0](https://dotnet.microsoft.com/download) or later.
- Windows, macOS, or Linux environment capable of running the .NET CLI.

## Restoring and building
```bash
cd CSharpFLPublic
dotnet restore
dotnet build
```

## Running the sample
`SBFLApp` accepts the solution directory, the test project name, the project under test, and optional boolean flags that control whether instrumentation should be reset and whether detailed test output is displayed. It instruments the discovered tests in place, executes them, aggregates the coverage data, and ranks statements by suspiciousness. Run the utility from the repository root:

```bash
dotnet run --project SBFLApp/SBFLApp.csproj . MathApp.Tests MathApp
```

Key behaviours:
- Test files are rewritten on disk during instrumentation. The tool removes previous instrumentation when rerun and updates the injected logging if necessary.
- Coverage GUIDs are written to `<FullyQualifiedTestName>.coverage` files inside a `Coverage/` directory created next to the running application. Each test first logs to `__sbfl_current_test.coverage.tmp` and the file is promoted when the run finishes so stale data is never mixed with new results.
- Once the run finishes, `suspiciousness_report.csv` is written to the provided solution directory.

To discard instrumentation and delete generated coverage artifacts, provide the optional reset flag:

```bash
dotnet run --project SBFLApp/SBFLApp.csproj . MathApp.Tests MathApp --reset
```

This cleans modified test sources, removes coverage files, and clears cached GUID mappings. Use `--verbose` (or pass `true` for the final argument) to stream the test output for each execution when additional verbosity is desired.

## Running the test suite
The repository includes traditional unit tests as well as instrumentation hooks. Execute all tests with:

```bash
dotnet test
```

The subtraction scenarios are expected to fail—they provide failing executions for the SBFL ranking.

## Extending the sample
- Introduce additional scenarios in `SeriesOperations` (or new helper classes) plus focused unit tests to explore how rankings shift when more complex bugs are introduced across multiple files.
- Experiment with additional spectrum-based metrics by following the patterns in `Rank.cs`.
- Integrate the ranking results with visualization tools or IDE extensions to aid debugging workflows.

