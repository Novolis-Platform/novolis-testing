namespace Novolis.Testing.Playwright;

/// <summary>One open, close, or step while walking a nestable section outline.</summary>
public sealed record PlaywrightWalkthroughOutlineOp(
    string Action,
    int Depth,
    string Heading,
    string Kind,
    PlaywrightWalkthroughStep? Step);
