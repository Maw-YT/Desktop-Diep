using System.Windows.Media;

namespace DesktopDiep;

public sealed class GameNotification
{
    public string Text = "";
    public Color Color;
    public double Life;
    public double MaxLife = 4;
    public string Id = "";
}

public sealed class NotificationSystem
{
    private readonly List<GameNotification> _items = [];

    public static readonly Color DeepRed = Color.FromRgb(0xBE, 0x3A, 0x3A);
    public static readonly Color DeepBlue = Color.FromRgb(0x2B, 0x4A, 0x9E);
    public static readonly Color Grey = Color.FromRgb(0x7A, 0x7A, 0x7A);
    public static readonly Color Pink = Color.FromRgb(0xE0, 0x5A, 0xA0);

    public IReadOnlyList<GameNotification> Items => _items;

    public void Push(string text, Color color, double seconds = 4.5, string id = "")
    {
        if (string.IsNullOrWhiteSpace(text))
            return;
        if (!string.IsNullOrEmpty(id))
        {
            for (var i = _items.Count - 1; i >= 0; i--)
            {
                if (_items[i].Id == id)
                    _items.RemoveAt(i);
            }
        }

        _items.Add(new GameNotification
        {
            Text = text,
            Color = color,
            Life = seconds,
            MaxLife = seconds,
            Id = id
        });
    }

    public void Server(string text, double seconds = 4.5, string id = "") =>
        Push(text, Grey, seconds, id);

    public void Arena(string text, double seconds = 6, string id = "arena") =>
        Push(text, DeepRed, seconds, id);

    public void Mode(string text, double seconds = 4.5, string id = "") =>
        Push(text, DeepBlue, seconds, id);

    public void Clear() => _items.Clear();

    public void Tick(double dt)
    {
        for (var i = _items.Count - 1; i >= 0; i--)
        {
            _items[i].Life -= dt;
            if (_items[i].Life <= 0)
                _items.RemoveAt(i);
        }
    }
}
