using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;

namespace SBFLApp
{
    public class Rank
    {
        public enum SuspiciousnessReportFormat
        {
            Csv,
            Markdown,
        }

        public sealed record SuspiciousnessReportRow(string StatementId, string DisplayName, IReadOnlyDictionary<string, float> Scores, float BestScore);

        public sealed record SuspiciousnessReportSnapshot(IReadOnlyList<string> Metrics, IReadOnlyList<SuspiciousnessReportRow> Rows);

        public Dictionary<string, float> tarantulaRank = [];
        public Dictionary<string, float> ochiaiRank = [];
        public Dictionary<string, float> dStarRank = [];
        public Dictionary<string, float> op2Rank = [];
        public Dictionary<string, float> jaccardRank = [];

        private readonly Dictionary<string, ISet<string>> testCoverage = [];
        private readonly Hashtable hashCoverage = [];
        private readonly Dictionary<string, bool> testPassFail = [];
        private readonly Hashtable hashPassFail = [];
        private readonly int totalFailed;
        private readonly int totalPassed;
        private readonly Dictionary<string, (int failed, int passed)> allStatements = [];
        private readonly HashSet<string> failed = [];
        private readonly HashSet<string> passed = [];

        public Rank(Dictionary<string, ISet<string>> testCoverage, Dictionary<string, bool> tastPassFail)
        {
            this.testCoverage = testCoverage;
            this.testPassFail = tastPassFail;

            hashCoverage = new Hashtable(testCoverage);
            HashSet<string> statements = [];

            foreach (var key1 in hashCoverage.Keys)
            {
                if (hashCoverage[key1] is ISet<string> statementSet) // Ensure type safety
                {
                    statements.UnionWith(statementSet);
                }
            }

            hashPassFail = new Hashtable(testPassFail);

            foreach (var key2 in hashPassFail.Keys)
            {
                if (key2 is string testName && hashPassFail[key2] is bool passFail) // Ensure type safety
                {
                    if (passFail)
                    {
                        passed.Add(testName);
                    }

                    else
                    {
                        failed.Add(testName);
                    }
                }
            }

            totalFailed = failed.Count;
            totalPassed = passed.Count;

            // Create a Dictionary where HashSet values are keys and a default value is assigned
            // For demonstration, we'll assign the length of the string as the value.
            allStatements = statements.ToDictionary(key => key, value => (0, 0));

            foreach (string stmt in allStatements.Keys)
            {
                allStatements[stmt] = GetCoverageCounts(stmt);
            }
        }

        /// <summary>
        /// Get the number of failing and passing tests covering this statement.
        /// </summary>
        /// <param name="stmt">The statement to check coverage for.</param>
        /// <returns>The number of failing and passing tests covering this statement.</returns>
        private (int failedCovered, int passedCovered) GetCoverageCounts(string stmt)
        {
            int failedOfStmt = 0;
            int passedOfStmt = 0;

            try
            {
                // Go through the failed tests and check to see if this statement is covered.
                foreach (string test in failed)
                {
                    ISet<string> statementCoveredByTest = testCoverage[test];
                    if (statementCoveredByTest.Contains(stmt))
                    {
                        failedOfStmt++;
                    }
                }

                // Go through the passing tests and check to see if this statement is covered.
                foreach (string test in passed)
                {
                    ISet<string> statementCoveredByTest = testCoverage[test];
                    if (statementCoveredByTest.Contains(stmt))
                    {
                        passedOfStmt++;
                    }
                }
            }
            catch (NullReferenceException nex)
            {
                Console.WriteLine(nex.StackTrace);
            }

            return (failedOfStmt, passedOfStmt);
        }

        /// <summary>
        /// Calculate the Tarantula SBFL score based on the current statement and it's coverage.
        /// </summary>
        public void CalculateTarantula()
        {
            foreach (string stmt in allStatements.Keys)
            {
                // Get the current statement statistics
                var statement = allStatements[stmt];

                float failedFraction = totalFailed > 0 ? (float)statement.failed / totalFailed : 0f;
                float passedFraction = totalPassed > 0 ? (float)statement.passed / totalPassed : 0f;
                float denominator = failedFraction + passedFraction;
                float rank = 0f;

                if (denominator > 0)
                {
                    rank = failedFraction / denominator;
                }

                tarantulaRank[stmt] = rank;
            }
        }

        /// <summary>
        /// Calculate the Ochiai SBFL score based on the current statement and it's coverage.
        /// </summary>
        public void CalculateOchiai()
        {
            foreach (string stmt in allStatements.Keys)
            { 
                // Get the current statement statistics
                var statement = allStatements[stmt];

                float denominator = (float)Math.Sqrt(totalFailed * (statement.failed + statement.passed));
                float rank = 0f;

                if (denominator > 0)
                {
                    rank = statement.failed / denominator;
                }

                ochiaiRank[stmt] = rank;

            }

        }

        /// <summary>
        /// Calculate the DStar SBFL score based on the current statement and it's coverage.
        /// </summary>
        public void CalculateDStar()
        {
            foreach (string stmt in allStatements.Keys)
            {
                // Get the current statement statistics
                var statement = allStatements[stmt];

                int denominatorBase = statement.passed + (totalFailed - statement.failed);
                float rank = 0f;

                if (denominatorBase > 0)
                {
                    rank = (float)(statement.failed * statement.failed) / denominatorBase;
                }
                else if (statement.failed > 0)
                {
                    rank = float.PositiveInfinity;
                }

                dStarRank[stmt] = rank;
            }
        }

        /// <summary>
        /// Calculate the Op2 SBFL score based on the current statement and it's coverage.
        /// </summary>
        public void CalculateOp2()
        {
            foreach (string stmt in allStatements.Keys)
            {
                // Get the current statement statistics
                var statement = allStatements[stmt];

                float rank = statement.failed - (float)statement.passed / (totalPassed + 1);

                op2Rank[stmt] = rank;
            }
        }

        /// <summary>
        /// Calculate the Jaccard SBFL score based on the current statement and it's coverage.
        /// </summary>
        public void CalculateJaccard()
        {
            foreach (string stmt in allStatements.Keys)
            {
                // Get the current statement statistics
                var statement = allStatements[stmt];

                int denominatorBase = totalFailed + statement.passed;
                float rank = 0f;

                if (denominatorBase > 0)
                {
                    rank = (float)statement.failed / denominatorBase;
                }

                jaccardRank[stmt] = rank;
            }
        }

        public SuspiciousnessReportSnapshot CreateReportSnapshot(int? topCount = null)
        {
            var calculatedColumns = GetCalculatedColumns();
            var rows = BuildRows(calculatedColumns);
            var filteredRows = ApplyTopFilter(rows, topCount);

            return new SuspiciousnessReportSnapshot(
                calculatedColumns.Select(column => column.Name).ToArray(),
                filteredRows);
        }

        public void WriteSuspiciousnessReport(string filePath, SuspiciousnessReportSnapshot snapshot, SuspiciousnessReportFormat format)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var writer = new StreamWriter(filePath, false);

            switch (format)
            {
                case SuspiciousnessReportFormat.Markdown:
                    WriteMarkdownReport(writer, snapshot);
                    break;
                default:
                    WriteCsvReport(writer, snapshot);
                    break;
            }
        }

        private static void WriteCsvReport(StreamWriter writer, SuspiciousnessReportSnapshot snapshot)
        {
            var headerColumns = new List<string> { "Statement" };
            headerColumns.AddRange(snapshot.Metrics);
            writer.WriteLine(string.Join(",", headerColumns));

            foreach (var row in snapshot.Rows)
            {
                var values = new List<string> { Escape($"{row.DisplayName}.{row.StatementId}") };
                values.AddRange(snapshot.Metrics.Select(metric => FormatRank(row.Scores.TryGetValue(metric, out float score) ? score : null)));

                writer.WriteLine(string.Join(",", values));
            }
        }

        private static void WriteMarkdownReport(StreamWriter writer, SuspiciousnessReportSnapshot snapshot)
        {
            var header = new List<string> { "Statement" };
            header.AddRange(snapshot.Metrics);

            writer.WriteLine($"| {string.Join(" | ", header)} |");
            writer.WriteLine($"| {string.Join(" | ", header.Select(_ => "---"))} |");

            foreach (var row in snapshot.Rows)
            {
                var values = new List<string> { EscapeMarkdown($"{row.DisplayName}.{row.StatementId}") };
                values.AddRange(snapshot.Metrics.Select(metric => EscapeMarkdown(FormatRank(row.Scores.TryGetValue(metric, out float score) ? score : null))));

                writer.WriteLine($"| {string.Join(" | ", values)} |");
            }
        }

        private List<(string Name, Dictionary<string, float> Ranks)> GetCalculatedColumns()
        {
            var rankColumns = new List<(string Name, Dictionary<string, float> Ranks)>
            {
                ("Tarantula", tarantulaRank),
                ("Ochiai", ochiaiRank),
                ("DStar", dStarRank),
                ("Op2", op2Rank),
                ("Jaccard", jaccardRank)
            };

            return rankColumns
                .Where(column => column.Ranks != null && column.Ranks.Count > 0)
                .ToList();
        }

        private List<SuspiciousnessReportRow> BuildRows(List<(string Name, Dictionary<string, float> Ranks)> calculatedColumns)
        {
            var guidMappings = GuidMappingStore.GetMappings();
            var orderedStatements = allStatements.Keys
                .Select(stmt =>
                {
                    var displayName = guidMappings.TryGetValue(stmt, out var methodName) ? methodName : stmt;
                    return new { Statement = stmt, Display = displayName };
                })
                .OrderBy(entry => entry.Display, StringComparer.Ordinal)
                .ThenBy(entry => entry.Statement, StringComparer.Ordinal)
                .ToList();

            var rows = new List<SuspiciousnessReportRow>(orderedStatements.Count);

            foreach (var entry in orderedStatements)
            {
                Dictionary<string, float> scores = [];

                foreach (var column in calculatedColumns)
                {
                    if (column.Ranks.TryGetValue(entry.Statement, out float value))
                    {
                        scores[column.Name] = value;
                    }
                }

                float bestScore = scores.Count == 0
                    ? float.NegativeInfinity
                    : scores.Values.Any(float.IsPositiveInfinity)
                        ? float.PositiveInfinity
                        : scores.Values.Max();

                rows.Add(new SuspiciousnessReportRow(entry.Statement, entry.Display, new ReadOnlyDictionary<string, float>(scores), bestScore));
            }

            return rows;
        }

        private static List<SuspiciousnessReportRow> ApplyTopFilter(List<SuspiciousnessReportRow> rows, int? topCount)
        {
            if (!topCount.HasValue || topCount.Value <= 0)
            {
                return rows;
            }

            return rows
                .OrderByDescending(row => float.IsPositiveInfinity(row.BestScore) ? float.PositiveInfinity : row.BestScore)
                .ThenBy(row => row.DisplayName, StringComparer.Ordinal)
                .ThenBy(row => row.StatementId, StringComparer.Ordinal)
                .Take(topCount.Value)
                .ToList();
        }

        private static string FormatRank(float? value)
        {
            if (!value.HasValue)
            {
                return string.Empty;
            }

            if (float.IsPositiveInfinity(value.Value))
            {
                return "Infinity";
            }

            return value.Value.ToString("F6", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Fix characters for CSV formatting.
        /// </summary>
        /// <param name="value">The string to adjust escape characters on.</param>
        /// <returns>The corrected string.</returns>
        private static string Escape(string value)
        {
            if (value.Contains(',') || value.Contains('"'))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }

        private static string EscapeMarkdown(string value)
        {
            return value
                .Replace("|", "\\|")
                .Replace("\n", " ");
        }

    }
}
