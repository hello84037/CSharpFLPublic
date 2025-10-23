# CSharpFL

CSharpFL is a small C# solution that demonstrates automated spectrum-based fault localization (SBFL). The included console utility instruments unit tests, executes them, gathers coverage, and ranks potentially faulty statements using several well-known metrics.

## Repository layout
- **MathApp/** – Simple console application used as the subject-under-test. `MathOperations.cs` exposes the arithmetic routines exercised by the tests.
- **MathApp.Tests/** – xUnit test project with addition and subtraction scenarios that trigger the instrumentation.
- **SBFLApp/** – Console application that performs the SBFL workflow.
  - `Program.cs` accepts the solution directory and test project name, injects logging into the matching test methods, runs the selected tests, and writes the coverage identifiers to disk.
  - `Rank.cs` calculates suspiciousness scores with the Tarantula, Ochiai, D*, Op2, and Jaccard formulas and exports the values to CSV.
- **MathApp.sln** – Solution file tying the application and test project together.

## Prerequisites
- [.NET SDK 8.0](https://dotnet.microsoft.com/download) or later.
- Windows, macOS, or Linux environment capable of running the .NET CLI.

## Restoring and building
```bash
cd CSharpFL
dotnet restore
dotnet build
```

## Running the sample
Executing the console application will instrument the sample tests, execute them, and compute fault localization rankings for the generated coverage data. Provide the solution directory and the name of the test project so the tool can locate and execute the tests dynamically.
```bash
dotnet run --project SBFLApp/SBFLApp.csproj . MathApp.Tests
```

After the run completes, the tool writes `suspiciousness_report.csv` to the supplied solution directory. Each line lists a statement identifier alongside its Tarantula, Ochiai, D*, Op2, and Jaccard scores.

## Running the test suite
The repository includes traditional unit tests as well as instrumentation hooks. Run the tests with:
```bash
dotnet test
```

## How fault localization works
1. **Instrumentation** – `SBFLApp.Program` uses Roslyn to insert logging statements into the configured test methods. Each execution writes a GUID to a `<TestClass>.<TestName>.coverage` file.
2. **Spectrum collection** – The application reads the generated coverage files into dictionaries describing which tests executed which statements.
3. **Ranking and export** – `Rank` processes the coverage spectrum and the pass/fail results to assign suspiciousness scores using Tarantula, Ochiai, D*, Op2, and Jaccard formulas, then exports the combined results to `suspiciousness_report.csv` for later inspection or visualization.

## Extending the sample
- Add new methods to `MathOperations` along with failing unit tests to explore how rankings shift when more complex bugs are introduced.
- Experiment with additional spectrum-based metrics by following the patterns in `Rank.cs`.
- Integrate the ranking results with visualization tools or IDE extensions to aid debugging workflows.
