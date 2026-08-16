namespace DesktopDiep;

internal sealed class NavGrid
{
    public const int Cell = 32;

    private bool[] _blocked = [];
    private int[] _came = [];
    private int[] _g = [];
    private int[] _seen = [];
    private int _stamp = 1;
    private int _cols;
    private int _rows;
    private readonly PriorityQueue<int, int> _open = new();
    private readonly List<int> _chain = [];
    private readonly List<(double X, double Y)> _tight = [];
    private readonly Queue<int> _bfs = new();

    public int Cols => _cols;
    public int Rows => _rows;
    public int BlockedCount { get; private set; }

    public bool CellBlocked(int cx, int cy) =>
        (uint)cx < (uint)_cols && (uint)cy < (uint)_rows && _blocked[cy * _cols + cx];

    public void Rebuild(double width, double height, IReadOnlyList<WindowBox> boxes, double inflate)
    {
        var cols = Math.Max(1, (int)Math.Ceiling(Math.Max(1, width) / Cell));
        var rows = Math.Max(1, (int)Math.Ceiling(Math.Max(1, height) / Cell));
        var n = cols * rows;
        if (_blocked.Length != n)
        {
            _blocked = new bool[n];
            _came = new int[n];
            _g = new int[n];
            _seen = new int[n];
        }
        _cols = cols;
        _rows = rows;
        Array.Clear(_blocked);
        var blockedCount = 0;
        const double minGap = 48;

        for (var y = 0; y < rows; y++)
        {
            var top = y * Cell;
            var bot = top + Cell;
            for (var x = 0; x < cols; x++)
            {
                var left = x * Cell;
                var right = left + Cell;
                var blocked = false;
                foreach (var box in boxes)
                {
                    if (CellHitsWindow(box, inflate, width, height, minGap, left, top, right, bot))
                    {
                        blocked = true;
                        break;
                    }
                }
                _blocked[y * cols + x] = blocked;
                if (blocked)
                    blockedCount++;
            }
        }
        BlockedCount = blockedCount;
    }

    private static bool CellHitsWindow(WindowBox box, double inflate, double width, double height, double minGap,
        double left, double top, double right, double bottom)
    {
        var l = box.Left - inflate;
        var t = box.Top - inflate;
        var r = box.Right + inflate;
        var b = box.Bottom + inflate;
        if (l < right && r > left && t < bottom && b > top)
            return true;
        if (l > 0 && l < minGap && left < l && right > 0 && t < bottom && b > top)
            return true;
        if (t > 0 && t < minGap && top < t && bottom > 0 && l < right && r > left)
            return true;
        if (r < width && width - r < minGap && left < width && right > r && t < bottom && b > top)
            return true;
        if (b < height && height - b < minGap && top < height && bottom > b && l < right && r > left)
            return true;
        return false;
    }

    public bool Occupied(double x, double y) => BlockedWorld(x, y);

    public bool ClearShot(double x0, double y0, double x1, double y1, double thick = 11)
    {
        if (!ClearLine(x0, y0, x1, y1))
            return false;
        var dx = x1 - x0;
        var dy = y1 - y0;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 8)
            return true;
        var nx = -dy / len * thick;
        var ny = dx / len * thick;
        return ClearLine(x0 + nx, y0 + ny, x1 + nx, y1 + ny)
            && ClearLine(x0 - nx, y0 - ny, x1 - nx, y1 - ny);
    }

    public bool ClearLine(double x0, double y0, double x1, double y1)
    {
        if (BlockedWorld(x0, y0) || BlockedWorld(x1, y1))
            return false;
        var dx = x1 - x0;
        var dy = y1 - y0;
        if (Math.Abs(dx) < 0.01 && Math.Abs(dy) < 0.01)
            return true;

        var cx = (int)Math.Floor(x0 / Cell);
        var cy = (int)Math.Floor(y0 / Cell);
        var gx = (int)Math.Floor(x1 / Cell);
        var gy = (int)Math.Floor(y1 / Cell);
        var stepX = Math.Sign(dx);
        var stepY = Math.Sign(dy);
        var adx = Math.Abs(dx);
        var ady = Math.Abs(dy);
        var tDeltaX = stepX == 0 ? double.PositiveInfinity : Cell / adx;
        var tDeltaY = stepY == 0 ? double.PositiveInfinity : Cell / ady;
        var tMaxX = stepX == 0
            ? double.PositiveInfinity
            : ((stepX > 0 ? (cx + 1) * Cell : cx * Cell) - x0) / dx;
        var tMaxY = stepY == 0
            ? double.PositiveInfinity
            : ((stepY > 0 ? (cy + 1) * Cell : cy * Cell) - y0) / dy;
        if (tMaxX < 0)
            tMaxX = 0;
        if (tMaxY < 0)
            tMaxY = 0;

        var guard = 0;
        while ((cx != gx || cy != gy) && guard++ < 4096)
        {
            const double eps = 1e-9;
            if (Math.Abs(tMaxX - tMaxY) < eps)
            {
                if (CornerBlocked(cx, cy, stepX, stepY))
                    return false;
                tMaxX += tDeltaX;
                tMaxY += tDeltaY;
                cx += stepX;
                cy += stepY;
            }
            else if (tMaxX < tMaxY)
            {
                tMaxX += tDeltaX;
                cx += stepX;
            }
            else
            {
                tMaxY += tDeltaY;
                cy += stepY;
            }
            if (OutOfGrid(cx, cy) || _blocked[cy * _cols + cx])
                return false;
        }
        return true;
    }

    private bool CornerBlocked(int cx, int cy, int stepX, int stepY)
    {
        if (stepX == 0 || stepY == 0)
            return false;
        var ax = cx + stepX;
        var by = cy + stepY;
        var side = OutOfGrid(ax, cy) || _blocked[cy * _cols + ax];
        var other = OutOfGrid(cx, by) || _blocked[by * _cols + cx];
        return side || other;
    }

    public bool TrySteer(double x, double y, double gx, double gy, List<(double X, double Y)> trail, out double ax, out double ay)
    {
        ax = 0;
        ay = 0;
        var wantX = gx;
        var wantY = gy;
        SnapGoal(ref gx, ref gy);
        trail.Clear();
        if (!BlockedWorld(x, y) && ClearLine(x, y, gx, gy))
        {
            trail.Add((x, y));
            trail.Add((gx, gy));
            ax = gx - x;
            ay = gy - y;
            return true;
        }

        var start = CellAt(x, y, walkable: true);
        var goal = CellAt(gx, gy, walkable: true);
        if (start == goal)
        {
            trail.Add((x, y));
            trail.Add((wantX, wantY));
            ax = wantX - x;
            ay = wantY - y;
            return ax != 0 || ay != 0;
        }

        if (!TryPath(start, goal, out var next))
        {
            trail.Add((x, y));
            trail.Add((wantX, wantY));
            ax = wantX - x;
            ay = wantY - y;
            return false;
        }
        WriteTrail(start, goal, x, y, trail);
        Tighten(trail);
        if (trail.Count >= 2)
        {
            ax = trail[1].X - x;
            ay = trail[1].Y - y;
        }
        else
        {
            CellCenter(next, out var nx, out var ny);
            ax = nx - x;
            ay = ny - y;
        }
        return true;
    }

    public void SnapGoal(ref double gx, ref double gy)
    {
        if (_cols == 0 || _rows == 0)
            return;
        gx = Math.Clamp(gx, Cell, Math.Max(Cell, _cols * Cell - Cell));
        gy = Math.Clamp(gy, Cell, Math.Max(Cell, _rows * Cell - Cell));
        var id = CellAt(gx, gy, walkable: true);
        if (Margin(id) >= 2 && !_blocked[id])
        {
            CellCenter(id, out gx, out gy);
            return;
        }
        var best = id;
        var bestScore = int.MinValue;
        var cx = id % _cols;
        var cy = id / _cols;
        for (var r = 1; r <= 10; r++)
        {
            for (var dy = -r; dy <= r; dy++)
            {
                for (var dx = -r; dx <= r; dx++)
                {
                    if (Math.Abs(dx) != r && Math.Abs(dy) != r)
                        continue;
                    var nx = cx + dx;
                    var ny = cy + dy;
                    if (OutOfGrid(nx, ny))
                        continue;
                    var n = ny * _cols + nx;
                    if (_blocked[n])
                        continue;
                    var score = Margin(n) * 1000 - (dx * dx + dy * dy);
                    if (score <= bestScore)
                        continue;
                    bestScore = score;
                    best = n;
                }
            }
            if (bestScore > int.MinValue && Margin(best) >= 2)
                break;
        }
        CellCenter(best, out gx, out gy);
    }

    public bool TryFiringPoint(double x, double y, double tx, double ty, double preferred, double maxRange,
        IReadOnlyList<WindowBox> windows, out double gx, out double gy)
    {
        gx = x;
        gy = y;
        if (_cols == 0)
            return false;
        if (WindowClear(windows, x, y, tx, ty))
        {
            var d = Math.Sqrt((tx - x) * (tx - x) + (ty - y) * (ty - y));
            if (d <= maxRange)
                return true;
        }

        BeginSearch();
        var start = CellAt(x, y, walkable: true);
        _g[start] = 0;
        _seen[start] = _stamp;
        _came[start] = start;
        _bfs.Enqueue(start);
        var best = -1;
        var bestScore = double.MaxValue;
        var visited = 0;
        var maxR2 = maxRange * maxRange;
        while (_bfs.Count > 0 && visited < 2800)
        {
            var cur = _bfs.Dequeue();
            visited++;
            CellCenter(cur, out var cx, out var cy);
            var dx = tx - cx;
            var dy = ty - cy;
            var d2 = dx * dx + dy * dy;
            if (d2 <= maxR2 && WindowClear(windows, cx, cy, tx, ty))
            {
                var d = Math.Sqrt(d2);
                var score = _g[cur] + Math.Abs(d - preferred) * 0.35;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = cur;
                    if (_g[cur] <= 4 && Math.Abs(d - preferred) < Cell * 2)
                        break;
                }
            }

            var ix = cur % _cols;
            var iy = cur / _cols;
            for (var dyi = -1; dyi <= 1; dyi++)
            {
                for (var dxi = -1; dxi <= 1; dxi++)
                {
                    if (dxi == 0 && dyi == 0)
                        continue;
                    var nx = ix + dxi;
                    var ny = iy + dyi;
                    if (OutOfGrid(nx, ny))
                        continue;
                    var id = ny * _cols + nx;
                    if (_blocked[id] || _seen[id] == _stamp)
                        continue;
                    if (dxi != 0 && dyi != 0 && (_blocked[iy * _cols + nx] || _blocked[ny * _cols + ix]))
                        continue;
                    _seen[id] = _stamp;
                    _g[id] = _g[cur] + (dxi == 0 || dyi == 0 ? 10 : 14);
                    _came[id] = cur;
                    _bfs.Enqueue(id);
                }
            }
        }

        if (best < 0)
            return false;
        CellCenter(best, out gx, out gy);
        return true;
    }

    private static bool WindowClear(IReadOnlyList<WindowBox> windows, double x0, double y0, double x1, double y1)
    {
        const double thick = 10;
        foreach (var box in windows)
        {
            if (box.HitsSegment(x0, y0, x1, y1))
                return false;
        }
        var dx = x1 - x0;
        var dy = y1 - y0;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 4)
            return true;
        var nx = -dy / len * thick;
        var ny = dx / len * thick;
        foreach (var box in windows)
        {
            if (box.HitsSegment(x0 + nx, y0 + ny, x1 + nx, y1 + ny)
                || box.HitsSegment(x0 - nx, y0 - ny, x1 - nx, y1 - ny))
                return false;
        }
        return true;
    }

    public bool TryOpenSpace(double x, double y, List<(double X, double Y)> trail, out double ax, out double ay)
    {
        ax = 0;
        ay = 0;
        trail.Clear();
        if (_cols == 0)
            return false;
        BeginSearch();
        var start = CellAt(x, y, walkable: true);
        _seen[start] = _stamp;
        _came[start] = start;
        _bfs.Enqueue(start);
        var found = -1;
        var visited = 0;
        while (_bfs.Count > 0 && visited < 1800)
        {
            var cur = _bfs.Dequeue();
            visited++;
            if (IsOpen(cur))
            {
                found = cur;
                break;
            }
            var ix = cur % _cols;
            var iy = cur / _cols;
            for (var dyi = -1; dyi <= 1; dyi++)
            {
                for (var dxi = -1; dxi <= 1; dxi++)
                {
                    if (dxi == 0 && dyi == 0)
                        continue;
                    var nx = ix + dxi;
                    var ny = iy + dyi;
                    if (OutOfGrid(nx, ny))
                        continue;
                    var id = ny * _cols + nx;
                    if (_blocked[id] || _seen[id] == _stamp)
                        continue;
                    if (dxi != 0 && dyi != 0 && (_blocked[iy * _cols + nx] || _blocked[ny * _cols + ix]))
                        continue;
                    _seen[id] = _stamp;
                    _came[id] = cur;
                    _bfs.Enqueue(id);
                }
            }
        }

        if (found < 0)
            return false;
        WriteTrail(start, found, x, y, trail);
        if (trail.Count >= 2)
        {
            ax = trail[1].X - x;
            ay = trail[1].Y - y;
            return true;
        }
        CellCenter(found, out var ox, out var oy);
        trail.Add((x, y));
        trail.Add((ox, oy));
        ax = ox - x;
        ay = oy - y;
        return true;
    }

    private bool IsOpen(int id)
    {
        var x = id % _cols;
        var y = id / _cols;
        if (_blocked[id])
            return false;
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0)
                    continue;
                var nx = x + dx;
                var ny = y + dy;
                if (OutOfGrid(nx, ny) || _blocked[ny * _cols + nx])
                    return false;
            }
        }
        return true;
    }

    private void BeginSearch()
    {
        _stamp++;
        if (_stamp == int.MaxValue)
        {
            Array.Clear(_seen);
            _stamp = 1;
        }
        _bfs.Clear();
    }

    private void WriteTrail(int start, int goal, double x, double y, List<(double X, double Y)> trail)
    {
        _chain.Clear();
        var cur = goal;
        var guard = 0;
        while (cur != start && _came[cur] != cur && guard++ < 1024)
        {
            _chain.Add(cur);
            cur = _came[cur];
        }
        trail.Add((x, y));
        CellCenter(start, out var sx, out var sy);
        if (Math.Abs(sx - x) > 2 || Math.Abs(sy - y) > 2)
            trail.Add((sx, sy));
        for (var i = _chain.Count - 1; i >= 0; i--)
        {
            CellCenter(_chain[i], out var cx, out var cy);
            trail.Add((cx, cy));
        }
    }

    private void Tighten(List<(double X, double Y)> trail)
    {
        if (trail.Count < 3)
            return;
        _tight.Clear();
        var i = 0;
        _tight.Add(trail[0]);
        while (i < trail.Count - 1)
        {
            var j = trail.Count - 1;
            while (j > i + 1 && !ClearLine(trail[i].X, trail[i].Y, trail[j].X, trail[j].Y))
                j--;
            _tight.Add(trail[j]);
            i = j;
        }
        trail.Clear();
        trail.AddRange(_tight);
    }

    private bool TryPath(int start, int goal, out int next)
    {
        next = start;
        _stamp++;
        if (_stamp == int.MaxValue)
        {
            Array.Clear(_seen);
            _stamp = 1;
        }

        _open.Clear();
        _g[start] = 0;
        _seen[start] = _stamp;
        _came[start] = start;
        _open.Enqueue(start, Heuristic(start, goal));

        var visited = 0;
        while (_open.Count > 0 && visited < 6000)
        {
            var cur = _open.Dequeue();
            visited++;
            if (cur == goal)
            {
                next = FirstStep(start, goal);
                return true;
            }

            var cx = cur % _cols;
            var cy = cur / _cols;
            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0)
                        continue;
                    var nx = cx + dx;
                    var ny = cy + dy;
                    if (OutOfGrid(nx, ny))
                        continue;
                    var id = ny * _cols + nx;
                    if (_blocked[id])
                        continue;
                    if (dx != 0 && dy != 0 && (_blocked[cy * _cols + nx] || _blocked[ny * _cols + cx]))
                        continue;

                    var step = dx == 0 || dy == 0 ? 10 : 14;
                    var prox = Margin(id);
                    if (prox == 0)
                        step += 36;
                    else if (prox == 1)
                        step += 16;
                    else if (prox == 2)
                        step += 6;
                    var g = _g[cur] + step;
                    if (_seen[id] == _stamp && g >= _g[id])
                        continue;
                    _seen[id] = _stamp;
                    _g[id] = g;
                    _came[id] = cur;
                    _open.Enqueue(id, g + Heuristic(id, goal));
                }
            }
        }

        return false;
    }

    private int FirstStep(int start, int goal)
    {
        var cur = goal;
        var prev = goal;
        var guard = 0;
        while (_came[cur] != cur && _came[cur] != start && guard++ < 1024)
        {
            prev = cur;
            cur = _came[cur];
        }
        return _came[cur] == start ? cur : prev;
    }

    private int Heuristic(int a, int b)
    {
        var ax = a % _cols;
        var ay = a / _cols;
        var bx = b % _cols;
        var by = b / _cols;
        var dx = Math.Abs(ax - bx);
        var dy = Math.Abs(ay - by);
        return 10 * (dx + dy) + 4 * Math.Min(dx, dy);
    }

    private int CellAt(double x, double y, bool walkable)
    {
        var cx = Math.Clamp((int)(x / Cell), 0, Math.Max(0, _cols - 1));
        var cy = Math.Clamp((int)(y / Cell), 0, Math.Max(0, _rows - 1));
        var id = cy * _cols + cx;
        if (!walkable || !_blocked[id])
            return id;
        var best = id;
        var bestScore = int.MinValue;
        for (var r = 1; r <= 8; r++)
        {
            for (var dy = -r; dy <= r; dy++)
            {
                for (var dx = -r; dx <= r; dx++)
                {
                    if (Math.Abs(dx) != r && Math.Abs(dy) != r)
                        continue;
                    var nx = cx + dx;
                    var ny = cy + dy;
                    if (OutOfGrid(nx, ny))
                        continue;
                    var n = ny * _cols + nx;
                    if (_blocked[n])
                        continue;
                    var score = Margin(n) * 400 - (dx * dx + dy * dy);
                    if (score <= bestScore)
                        continue;
                    bestScore = score;
                    best = n;
                }
            }
            if (bestScore > int.MinValue)
                return best;
        }
        return id;
    }

    private void CellCenter(int id, out double x, out double y)
    {
        x = (id % _cols + 0.5) * Cell;
        y = (id / _cols + 0.5) * Cell;
    }

    private int Margin(int id)
    {
        var x = id % _cols;
        var y = id / _cols;
        return Math.Min(Math.Min(x, y), Math.Min(_cols - 1 - x, _rows - 1 - y));
    }

    private bool OutOfGrid(int x, int y) => (uint)x >= (uint)_cols || (uint)y >= (uint)_rows;

    private bool BlockedWorld(double x, double y)
    {
        if (_cols == 0 || _rows == 0)
            return false;
        var cx = (int)Math.Floor(x / Cell);
        var cy = (int)Math.Floor(y / Cell);
        if (OutOfGrid(cx, cy))
            return true;
        return _blocked[cy * _cols + cx];
    }
}
