using DesktopDiep;

var root = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
var assets = Path.Combine(root, "Assets");
AppIcon.WriteAssets(assets);
Console.WriteLine($"Wrote {Path.Combine(assets, "app.png")}");
Console.WriteLine($"Wrote {Path.Combine(assets, "app.ico")}");
