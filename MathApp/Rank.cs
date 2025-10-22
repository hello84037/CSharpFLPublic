using System;
using System.Collections;
using System.Collections.Generic;

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

            float denominator = (float)Math.Sqrt(totalFailed * (failedOfStmt + passedOfStmt));
            float rank = 0f;

            if (denominator > 0)
            {
                rank = failedOfStmt / denominator;
            }

            ochiaiRank.Add(stmt, rank);
            Console.WriteLine($"[Ochiai] Statement: {stmt}, Rank: {rank:F3}");
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

            ochiaiRank.Add(stmt, rank);
            Console.WriteLine($"[Ochiai] Statement: {stmt}, Rank: {rank:F3}");

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

            dStarRank.Add(stmt, rank);
            Console.WriteLine($"[D*] Statement: {stmt}, Rank: {rank:F3}");
        }
    }

    public void calculateOp2()
    {
        foreach (string stmt in allStatements)
        {
            (int failedOfStmt, int passedOfStmt) = GetCoverageCounts(stmt);

            float rank = failedOfStmt - (float)passedOfStmt / (totalPassed + 1);

            op2Rank.Add(stmt, rank);
            Console.WriteLine($"[Op2] Statement: {stmt}, Rank: {rank:F3}");
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

            jaccardRank.Add(stmt, rank);
            Console.WriteLine($"[Jaccard] Statement: {stmt}, Rank: {rank:F3}");
        }
    }


}
