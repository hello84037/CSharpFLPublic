namespace SBFLApp
{
    internal static class GuidMappingStore
    {
        private static readonly object SyncRoot = new();
        private static readonly string MappingFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "guid_method_map.csv");
        private static Dictionary<string, GuidMappingEntry>? _mappings;

        private sealed record GuidMappingEntry(string MethodName, string? SourceFile)
        {
            public override string ToString()
            {
                return string.IsNullOrWhiteSpace(SourceFile)
                    ? MethodName
                    : $"({SourceFile}) {MethodName}";
            }
        }

        public static void AddMapping(string guid, string methodName, string? sourceFile)
        {
            if (string.IsNullOrWhiteSpace(guid) || string.IsNullOrWhiteSpace(methodName))
            {
                return;
            }

            lock (SyncRoot)
            {
                EnsureLoaded();

                var normalizedGuid = guid.Trim();
                var normalizedMethod = methodName.Trim();
                var normalizedSource = string.IsNullOrWhiteSpace(sourceFile) ? null : sourceFile.Trim();

                if (_mappings!.TryGetValue(normalizedGuid, out var existing) &&
                    string.Equals(existing.MethodName, normalizedMethod, StringComparison.Ordinal) &&
                    string.Equals(existing.SourceFile, normalizedSource, StringComparison.Ordinal))
                {
                    return;
                }

                _mappings[normalizedGuid] = new GuidMappingEntry(normalizedMethod, normalizedSource);
                Persist();
            }
        }

        public static IReadOnlyDictionary<string, string> GetMappings()
        {
            lock (SyncRoot)
            {
                EnsureLoaded();
                return _mappings!.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.ToString(),
                    StringComparer.Ordinal
                );
            }
        }

        public static void Clear()
        {
            lock (SyncRoot)
            {
                _mappings = new Dictionary<string, GuidMappingEntry>(StringComparer.Ordinal);

                if (File.Exists(MappingFilePath))
                {
                    try
                    {
                        File.Delete(MappingFilePath);
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"Failed to delete mapping file '{MappingFilePath}': {ex.Message}");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Console.WriteLine($"Failed to delete mapping file '{MappingFilePath}': {ex.Message}");
                    }
                }
            }
        }

        public static bool TryGetMethodName(string guid, out string? methodName)
        {
            lock (SyncRoot)
            {
                EnsureLoaded();
                if (_mappings!.TryGetValue(guid, out var entry))
                {
                    methodName = entry.ToString();
                    return true;
                }
            }

            methodName = null;
            return false;
        }

        private static void EnsureLoaded()
        {
            if (_mappings != null)
            {
                return;
            }

            var map = new Dictionary<string, GuidMappingEntry>(StringComparer.Ordinal);

            if (File.Exists(MappingFilePath))
            {
                foreach (var line in File.ReadLines(MappingFilePath))
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    var parts = line.Split(',', 3);
                    if (parts.Length >= 2)
                    {
                        var guid = parts[0].Trim();
                        var methodName = parts[1].Trim();
                        string? sourceFile = null;

                        if (parts.Length == 3)
                        {
                            var filePart = parts[2].Trim();
                            sourceFile = string.IsNullOrWhiteSpace(filePart) ? null : filePart;
                        }

                        if (!string.IsNullOrWhiteSpace(guid) && !string.IsNullOrWhiteSpace(methodName))
                        {
                            map[guid] = new GuidMappingEntry(methodName, sourceFile);
                        }
                    }
                }
            }

            _mappings = map;
        }

        private static void Persist()
        {
            if (_mappings == null)
            {
                return;
            }

            var lines = _mappings.Select(pair =>
            {
                var sourceFilePart = string.IsNullOrWhiteSpace(pair.Value.SourceFile) ? string.Empty : pair.Value.SourceFile;
                return string.Join(',', pair.Key, pair.Value.MethodName, sourceFilePart);
            });

            File.WriteAllLines(MappingFilePath, lines);
        }
    }
}
