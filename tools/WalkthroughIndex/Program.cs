using Novolis.Testing.Playwright;

var root = args.Length > 0
    ? Path.GetFullPath(args[0])
    : PlaywrightArtifactStore.ResolveRoot();
var entries = PlaywrightWalkthroughCatalog.Refresh(root);
Console.WriteLine($"{entries.Count} latest walkthroughs in {root}");
foreach (var entry in entries)
{
    Console.WriteLine($"  {entry.ClassName} / {entry.Title} ({entry.StepCount} steps)");
}

Console.WriteLine(Path.Combine(root, PlaywrightWalkthroughCatalog.HtmlFileName));
