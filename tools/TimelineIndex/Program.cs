using Novolis.Testing.Playwright;
using Novolis.Testing.Timeline;

var root = args.Length > 0
    ? Path.GetFullPath(args[0])
    : PlaywrightArtifactStore.ResolveRoot();
var entries = TimelineCatalog.Refresh(root);
Console.WriteLine($"{entries.Count} latest timelines in {root}");
foreach (var entry in entries)
{
    Console.WriteLine($"  {entry.ClassName} / {entry.Title} ({entry.StepCount} steps)");
}

Console.WriteLine(Path.Combine(root, TimelineCatalog.HtmlFileName));
