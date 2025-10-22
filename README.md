# CSharpFL

CSharpFL is a small C# project that demonstrates automated spectrum-based fault localization. It instruments unit tests to capture statement coverage and ranks potentially faulty statements using several well-known metrics.

## Repository layout
- **MathApp/** – Core console application used as a target for instrumentation.
  - `Program.cs` injects logging statements into test methods, executes them, and gathers coverage identifiers.
  - `MathOperations.cs` holds the simple arithmetic functions that the tests exercise.
  - `Spectrum.cs` contains helpers for inserting instrumentation into source files and projects.
  - `Rank.cs` calculates suspiciousness scores with Tarantula, Ochiai, D*, Op2, and Jaccard formulas.
- **MathApp.Tests/** – xUnit test project with addition and subtraction scenarios that generate coverage data.
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
Executing the console application will instrument the sample tests and compute fault localization rankings for the generated coverage data.
```bash
dotnet run --project MathApp/MathApp.csproj
```

## Running the test suite
The repository includes traditional unit tests as well as instrumentation hooks. Run the tests with:
```bash
dotnet test
```

## How fault localization works
1. **Instrumentation** – `Program.SetInjection` leverages Roslyn to insert logging statements into the test methods listed in `targets`. Each execution writes a GUID to a `<TestClass>.<TestName>.coverage` file.
2. **Spectrum collection** – The application reads the generated coverage files into dictionaries describing which tests executed which statements.
3. **Ranking** – `Rank` processes the coverage spectrum and the pass/fail results to assign suspiciousness scores using Tarantula, Ochiai, D*, Op2, and Jaccard formulas. The resulting dictionaries can be used to inspect or visualize potential fault locations.

## Extending the sample
- Add new methods to `MathOperations` along with failing unit tests to explore how rankings shift when more complex bugs are introduced.
- Experiment with additional spectrum-based metrics by following the patterns in `Rank.cs`.
- Integrate the ranking results with visualization tools or IDE extensions to aid debugging workflows.
