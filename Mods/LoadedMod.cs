using System.IO;
using MoonSharp.Interpreter;

namespace DesktopDiep;

internal sealed class ModListEntry
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Version { get; init; } = "";
    public string Author { get; init; } = "";
    public required string Folder { get; init; }
    public bool Enabled { get; init; }
    public string? Error { get; init; }
    public bool CanToggle { get; init; } = true;

    public string MenuLabel
    {
        get
        {
            var label = string.IsNullOrWhiteSpace(Version) ? Name : $"{Name}  v{Version}";
            if (Enabled)
                return label;
            return string.IsNullOrWhiteSpace(Error) ? $"{label}  (off)" : $"{label}  (error)";
        }
    }

    public string Tooltip
    {
        get
        {
            var author = string.IsNullOrWhiteSpace(Author) ? "" : $" by {Author}";
            if (Enabled)
                return $"{Id}{author}";
            return string.IsNullOrWhiteSpace(Error)
                ? $"{Id}{author}"
                : $"{Id}{author}\n{Error}";
        }
    }
}

internal sealed class LoadedMod
{
    public required ModManifest Manifest { get; init; }
    public required string Folder { get; init; }
    public required Script Script { get; init; }
    public bool Enabled { get; set; } = true;
    public string? LastError { get; private set; }

    public string DataPath => Path.Combine(Folder, "data");

    public void Fail(Exception ex)
    {
        Enabled = false;
        LastError = ex.Message;
        ModHost.Current?.ReportModError(this, ex);
    }
}
