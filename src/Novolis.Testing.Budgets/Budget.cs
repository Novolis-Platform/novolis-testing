namespace Novolis.Testing.Budgets;

/// <summary>Ceilings applied to one <see cref="BudgetSample"/>.</summary>
/// <param name="MaxElapsed">Maximum measured elapsed time. Unset ceilings are not checked.</param>
/// <param name="MaxAllocatedBytes">Maximum allocated bytes. Unset ceilings are not checked.</param>
/// <param name="MinThroughput">Minimum throughput in the sample unit. Unset ceilings are not checked.</param>
public sealed record Budget(
    TimeSpan? MaxElapsed = null,
    long? MaxAllocatedBytes = null,
    double? MinThroughput = null);
