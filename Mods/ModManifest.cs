using System.IO;
using System.Text.Json;

namespace DesktopDiep;

internal sealed class ModManifest
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    public string Author { get; set; } = "";
    public string Entry { get; set; } = "main.lua";

    public static ModManifest? TryLoad(string folder)
    {
        var path = Path.Combine(folder, "mod.json");
        if (!File.Exists(path))
            return null;
        try
        {
            var json = File.ReadAllText(path);
            var m = JsonSerializer.Deserialize<ModManifest>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (m is null || string.IsNullOrWhiteSpace(m.Id))
                return null;
            if (string.IsNullOrWhiteSpace(m.Name))
                m.Name = m.Id;
            if (string.IsNullOrWhiteSpace(m.Entry))
                m.Entry = "main.lua";
            return m;
        }
        catch
        {
            return null;
        }
    }
}
