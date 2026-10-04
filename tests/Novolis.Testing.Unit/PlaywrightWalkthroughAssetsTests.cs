using Novolis.Testing.Playwright;

namespace Novolis.Testing.Unit;

public sealed class PlaywrightWalkthroughAssetsTests
{
    private static readonly byte[] OnePixelPng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
        0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
        0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
        0x42, 0x60, 0x82,
    ];

    [Test]
    public async Task ToDataUri_reads_png_as_image_png()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-walkthrough-assets", Guid.NewGuid().ToString("N"));
        var frames = Path.Combine(root, "frames");
        Directory.CreateDirectory(frames);
        await File.WriteAllBytesAsync(Path.Combine(frames, "01.png"), OnePixelPng);

        var uri = PlaywrightWalkthroughAssets.ToDataUri(root, "frames/01.png");

        await Assert.That(uri).StartsWith("data:image/png;base64,");
        await Assert.That(Convert.FromBase64String(uri["data:image/png;base64,".Length..])).IsEquivalentTo(OnePixelPng);
    }

    [Test]
    public async Task ToDataUriOrPath_keeps_missing_frames_as_paths()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-walkthrough-assets", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        await Assert.That(PlaywrightWalkthroughAssets.ToDataUriOrPath(root, "frames/01.png"))
            .IsEqualTo("frames/01.png");
    }
}
