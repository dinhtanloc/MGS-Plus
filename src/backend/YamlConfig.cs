using YamlDotNet.Serialization;

namespace MGSPlus.Api;

/// <summary>
/// Reads non-secret defaults from configs/*.yml.
/// Priority: environment variable > YAML value > hardcoded fallback.
/// </summary>
public static class YamlConfig
{
    private static readonly string? _root = FindConfigsRoot();
    private static readonly Dictionary<string, Dictionary<object, object>> _cache = new();

    private static string? FindConfigsRoot()
    {
        // Walk up from the binary location until we find a "configs" directory.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "configs")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static Dictionary<object, object> Load(string filename)
    {
        if (_cache.TryGetValue(filename, out var cached))
            return cached;

        if (_root == null)
            return _cache[filename] = [];

        var path = Path.Combine(_root, "configs", filename);
        if (!File.Exists(path))
            return _cache[filename] = [];

        var deserializer = new DeserializerBuilder().Build();
        var result = deserializer.Deserialize<Dictionary<object, object>>(
            File.ReadAllText(path));
        return _cache[filename] = result ?? [];
    }

    /// <summary>
    /// Returns env var if set; otherwise the YAML value at the given dot-separated key path;
    /// otherwise the hardcoded fallback.
    /// </summary>
    public static string Get(string envVar, string filename, string keyPath, string fallback = "")
    {
        var env = Environment.GetEnvironmentVariable(envVar);
        if (!string.IsNullOrEmpty(env)) return env;

        var value = Navigate(Load(filename), keyPath.Split('.'));
        return value?.ToString() ?? fallback;
    }

    /// <summary>
    /// Returns all string items from a YAML sequence at the given key path.
    /// </summary>
    public static string[] GetList(string filename, string keyPath)
    {
        var value = Navigate(Load(filename), keyPath.Split('.'));
        if (value is List<object> list)
            return list.Select(x => x?.ToString() ?? "")
                       .Where(s => !string.IsNullOrEmpty(s))
                       .ToArray();
        return [];
    }

    private static object? Navigate(Dictionary<object, object> data, string[] keys)
    {
        object? node = data;
        foreach (var key in keys)
        {
            if (node is not Dictionary<object, object> dict) return null;
            if (!dict.TryGetValue(key, out node)) return null;
        }
        return node;
    }
}
