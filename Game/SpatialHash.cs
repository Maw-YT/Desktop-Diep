namespace DesktopDiep;

internal sealed class SpatialHash
{
    public const double CellSize = 64;

    private readonly Dictionary<long, List<int>> _cells = new(256);
    private readonly Stack<List<int>> _pool = new();
    private readonly HashSet<long> _seen = [];

    public int CellCount => _cells.Count;
    public int PairCount { get; private set; }

    public void ForEachCell(Action<int, int, int> visit)
    {
        foreach (var (key, list) in _cells)
            visit((int)(key >> 32), (int)(uint)key, list.Count);
    }

    public void Clear()
    {
        foreach (var list in _cells.Values)
        {
            list.Clear();
            _pool.Push(list);
        }
        _cells.Clear();
        _seen.Clear();
        PairCount = 0;
    }

    public void Insert(int id, double x, double y, double radius)
    {
        var minX = Floor((x - radius) / CellSize);
        var maxX = Floor((x + radius) / CellSize);
        var minY = Floor((y - radius) / CellSize);
        var maxY = Floor((y + radius) / CellSize);
        for (var cy = minY; cy <= maxY; cy++)
        {
            for (var cx = minX; cx <= maxX; cx++)
            {
                var key = Pack(cx, cy);
                if (!_cells.TryGetValue(key, out var list))
                {
                    list = _pool.Count > 0 ? _pool.Pop() : new List<int>(8);
                    _cells[key] = list;
                }
                list.Add(id);
            }
        }
    }

    public void ForEachPair(Action<int, int> visit)
    {
        foreach (var list in _cells.Values)
        {
            var n = list.Count;
            if (n < 2)
                continue;
            for (var i = 0; i < n - 1; i++)
            {
                var a = list[i];
                for (var j = i + 1; j < n; j++)
                {
                    var b = list[j];
                    if (a == b)
                        continue;
                    var lo = a < b ? a : b;
                    var hi = a < b ? b : a;
                    var pair = ((long)lo << 32) | (uint)hi;
                    if (!_seen.Add(pair))
                        continue;
                    PairCount++;
                    visit(lo, hi);
                }
            }
        }
    }

    private static long Pack(int x, int y) => ((long)x << 32) ^ (uint)y;

    private static int Floor(double v) => (int)Math.Floor(v);
}
