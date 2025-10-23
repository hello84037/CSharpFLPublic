using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SBFLApp;

public class Rank
{
    Dictionary<string, ISet<string>> testCoverage;
    Hashtable hashCoverage;
    Dictionary<string, bool> testPassFail;
    Hashtable hashPassFail;
    int totalFailed;
    int totalPassed;
    ISet<string> allStatements;
    ISet<string> failed;
    ISet<string> passed;
    public Dictionary<string, float> tarantulaRank;
    public Dictionary<string, float> ochiaiRank;
    public Dictionary<string, float> dStarRank;
    public Dictionary<string, float> op2Rank;
    public Dictionary<string, float> jaccardRank;
    List<string> finalRanking;

    public Rank(Dictionary<string, ISet<string>> testCoverage, Dictionary<string, bool> tastPassFail)
    {
        this.testCoverage = testCoverage;
        this.testPassFail = tastPassFail;
        allStatements = new HashSet<string>();
        failed = new HashSet<string>();
        passed = new HashSet<string>();
        tarantulaRank = new Dictionary<string, float>();
        ochiaiRank = new Dictionary<string, float>();
        dStarRank = new Dictionary<string, float>();
        op2Rank = new Dictionary<string, float>();
        jaccardRank = new Dictionary<string, float>();
        finalRanking = new List<string>();

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

    public void calculateTarantula()
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

    public void calculateOchiai()
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

    public void calculateDStar()
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

    public void calculateOp2()
    {
        foreach (string stmt in allStatements)
        {
            (int failedOfStmt, int passedOfStmt) = GetCoverageCounts(stmt);

            float rank = failedOfStmt - (float)passedOfStmt / (totalPassed + 1);

            op2Rank[stmt] = rank;
        }
    }

    public void calculateJaccard()
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

        using var writer = new StreamWriter(filePath, false);
        writer.WriteLine("Statement,Tarantula,Ochiai,DStar,Op2,Jaccard");

        foreach (var entry in orderedStatements)
        {
            string tarantulaValue = FormatRank(tarantulaRank, entry.Statement);
            string ochiaiValue = FormatRank(ochiaiRank, entry.Statement);
            string dStarValue = FormatRank(dStarRank, entry.Statement);
            string op2Value = FormatRank(op2Rank, entry.Statement);
            string jaccardValue = FormatRank(jaccardRank, entry.Statement);

            writer.WriteLine($"{Escape(entry.Display)},{tarantulaValue},{ochiaiValue},{dStarValue},{op2Value},{jaccardValue}");
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
