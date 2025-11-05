using System.Collections;
using System.Globalization;

namespace SBFLApp
{
    public class Rank
    {
        public Dictionary<string, float> tarantulaRank;
        public Dictionary<string, float> ochiaiRank;
        public Dictionary<string, float> dStarRank;
        public Dictionary<string, float> op2Rank;
        public Dictionary<string, float> jaccardRank;

        private readonly Dictionary<string, ISet<string>> testCoverage;
        private readonly Hashtable hashCoverage;
        private readonly Dictionary<string, bool> testPassFail;
        private readonly Hashtable hashPassFail;
        private readonly int totalFailed;
        private readonly int totalPassed;
        private readonly HashSet<string> allStatements;
        private readonly HashSet<string> failed;
        private readonly HashSet<string> passed;
        private readonly List<string> finalRanking;

        public Rank(Dictionary<string, ISet<string>> testCoverage, Dictionary<string, bool> tastPassFail)
        {
            this.testCoverage = testCoverage;
            this.testPassFail = tastPassFail;
            allStatements = [];
            failed = [];
            passed = [];
            tarantulaRank = [];
            ochiaiRank = [];
            dStarRank = [];
            op2Rank = [];
            jaccardRank = [];
            finalRanking = [];

            hashCoverage = new Hashtable(testCoverage);


            foreach (var key1 in hashCoverage.Keys)
            {
                if (hashCoverage[key1] is ISet<string> statementSet) // Ensure type safety
                {
                    allStatements.UnionWith(statementSet);
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
        }

        private (int failedCovered, int passedCovered) GetCoverageCounts(string stmt)
        {
            int failedOfStmt = 0;
            int passedOfStmt = 0;

            try
            {
                foreach (string test in failed)
                {
                    ISet<string> statementCoveredByTest = testCoverage[test];
                    if (statementCoveredByTest.Contains(stmt))
                    {
                        failedOfStmt++;
                    }
                }

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

        public void CalculateTarantula()
        {
            foreach (string stmt in allStatements)
            {
                (int failedOfStmt, int passedOfStmt) = GetCoverageCounts(stmt);

                float failedFraction = totalFailed > 0 ? (float)failedOfStmt / totalFailed : 0f;
                float passedFraction = totalPassed > 0 ? (float)passedOfStmt / totalPassed : 0f;
                float denominator = failedFraction + passedFraction;
                float rank = 0f;

                if (denominator > 0)
                {
                    rank = failedFraction / denominator;
                }

                tarantulaRank[stmt] = rank;
            }
        }

        public void CalculateOchiai()
        {
            foreach (string stmt in allStatements)
            {
                (int failedOfStmt, int passedOfStmt) = GetCoverageCounts(stmt);

                float denominator = (float)Math.Sqrt(totalFailed * (failedOfStmt + passedOfStmt));
                float rank = 0f;

                if (denominator > 0)
                {
                    rank = failedOfStmt / denominator;
                }

                ochiaiRank[stmt] = rank;

            }

        }

        public void CalculateDStar()
        {
            foreach (string stmt in allStatements)
            {
                (int failedOfStmt, int passedOfStmt) = GetCoverageCounts(stmt);

                int denominatorBase = passedOfStmt + (totalFailed - failedOfStmt);
                float rank = 0f;

                if (denominatorBase > 0)
                {
                    rank = (float)(failedOfStmt * failedOfStmt) / denominatorBase;
                }
                else if (failedOfStmt > 0)
                {
                    rank = float.PositiveInfinity;
                }

                dStarRank[stmt] = rank;
            }
        }

        public void CalculateOp2()
        {
            foreach (string stmt in allStatements)
            {
                (int failedOfStmt, int passedOfStmt) = GetCoverageCounts(stmt);

                float rank = failedOfStmt - (float)passedOfStmt / (totalPassed + 1);

                op2Rank[stmt] = rank;
            }
        }

        public void CalculateJaccard()
        {
            foreach (string stmt in allStatements)
            {
                (int failedOfStmt, int passedOfStmt) = GetCoverageCounts(stmt);

                int denominatorBase = totalFailed + passedOfStmt;
                float rank = 0f;

                if (denominatorBase > 0)
                {
                    rank = (float)failedOfStmt / denominatorBase;
                }

                jaccardRank[stmt] = rank;
            }
        }

        public void WriteSuspiciousnessReport(string filePath)
        {
            var guidMappings = GuidMappingStore.GetMappings();
            var orderedStatements = allStatements
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
                var values = new List<string> { Escape(entry.Display) };
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
