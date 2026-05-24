namespace Novolis.Testing.TestBases;

/// <summary>Options controlling test host startup behavior.</summary>
public class TestOptions
{
    /// <summary>
    /// Will actually run the host as a running application
    /// </summary>
    public bool StartHost { get; set; } = false;
}
