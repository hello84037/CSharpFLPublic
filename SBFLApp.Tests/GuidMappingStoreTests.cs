using SBFLApp;

namespace SBFLApp.Tests;

public sealed class GuidMappingStoreTests : IDisposable
{
    public GuidMappingStoreTests()
    {
        GuidMappingStore.Clear();
    }

    [Fact]
    public void AddMapping_StoresTrimmedValues()
    {
        const string guid = " 12345678-1234-1234-1234-1234567890ab ";
        const string methodName = "  SampleMethod  ";
        const string sourceFile = "  Sample.cs  ";

        GuidMappingStore.AddMapping(guid, methodName, sourceFile);

        Assert.True(GuidMappingStore.TryGetMethodName("12345678-1234-1234-1234-1234567890ab", out var stored));
        Assert.Equal("(Sample.cs) SampleMethod", stored);

        var mappings = GuidMappingStore.GetMappings();
        Assert.Single(mappings);
        Assert.Equal("(Sample.cs) SampleMethod", mappings["12345678-1234-1234-1234-1234567890ab"]);
    }

    [Fact]
    public void Flush_PersistsMappingsToFile()
    {
        var guid = Guid.NewGuid().ToString();
        const string methodName = "PersistedMethod";

        GuidMappingStore.AddMapping(guid, methodName, null);
        GuidMappingStore.Flush();

        var mappingFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "guid_method_map.csv");
        Assert.True(File.Exists(mappingFile));

        var lines = File.ReadAllLines(mappingFile);
        Assert.Contains(lines, line => line.StartsWith(guid, StringComparison.Ordinal) &&
                                        line.Contains("," + methodName + ",", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        GuidMappingStore.Clear();
    }
}
