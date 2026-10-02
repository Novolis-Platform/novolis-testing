namespace Novolis.Testing.Budgets;

/// <summary>Which allocation counter a budget probe reads.</summary>
public enum AllocationScope
{
    /// <summary>Bytes allocated on the thread that runs a synchronous body.</summary>
    CurrentThread = 0,

    /// <summary>
    /// Process-wide bytes from <see cref="GC.GetTotalAllocatedBytes(bool)"/>.
    /// Required for async bodies. The test should be marked not-in-parallel.
    /// </summary>
    Process = 1,
}
