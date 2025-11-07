using System.Collections;
using System.Globalization;

namespace SBFLApp
{
    public class Rank
    {
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

        public void WriteSuspiciousnessReport(string filePath)
        {
            var guidMappings = GuidMappingStore.GetMappings();
            var orderedStatements = allStatements.Keys
                .Select(stmt =>
                {
                    var displayName = guidMappings.TryGetValue(stmt, out var methodName) ? methodName : stmt;
                    return new { Statement = stmt, Display = displayName };
                })
                .OrderBy(entry => entry.Display, StringComparer.Ordinal);

            var rankColumns = new List<(string Name, Dictionary<string, float> Ranks)>
            {
                ("Tarantula", tarantulaRank),
                ("Ochiai", ochiaiRank),
                ("DStar", dStarRank),
                ("Op2", op2Rank),
                ("Jaccard", jaccardRank)
            };

            var calculatedColumns = rankColumns
                .Where(column => column.Ranks != null && column.Ranks.Count > 0)
                .ToList();

            using var writer = new StreamWriter(filePath, false);
            var headerColumns = new List<string> { "Statement" };
            headerColumns.AddRange(calculatedColumns.Select(column => column.Name));
            writer.WriteLine(string.Join(",", headerColumns));

            foreach (var entry in orderedStatements)
            {
                var values = new List<string> { Escape($"{entry.Display}.{entry.Statement}") };
                values.AddRange(calculatedColumns.Select(column => FormatRank(column.Ranks, entry.Statement)));

                writer.WriteLine(string.Join(",", values));
            }
        }

        private static string FormatRank(Dictionary<string, float> ranks, string stmt)
        {
            if (ranks.TryGetValue(stmt, out float value))
            {
                if (float.IsPositiveInfinity(value))
                {
                    return "Infinity";
                }

                return value.ToString("F6", CultureInfo.InvariantCulture);
            }

            return string.Empty;
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

    }
}
