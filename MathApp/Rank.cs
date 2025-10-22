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

    public void calculateTarantula()
    {
        foreach (string stmt in allStatements)
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

            float failedPart = (float)failedOfStmt / totalFailed;
            float passedPart = (float)passedOfStmt / totalPassed;

            float rank = failedPart / (failedPart + passedPart);

            tarantulaRank.Add(stmt, rank);
            Console.WriteLine($"[Tarantula] Statement: {stmt}, Rank: {rank:F3}");

        }


    }

    public void calculateOchiai()
    {
        foreach (string stmt in allStatements)
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
}
