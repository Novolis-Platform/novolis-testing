using System.Reflection;

namespace Novolis.Testing.Coverage;

/// <summary>
/// Enumerates public API surface for smoke / coverage-closing tests.
/// Libraries own behavior; hosts and CLIs stay thin.
/// </summary>
public static class PublicApiSurface
{
    /// <summary>Public types declared by <paramref name="assembly"/> (excludes compiler-generated).</summary>
    public static IReadOnlyList<Type> PublicTypes(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return assembly.GetExportedTypes()
            .Where(t => t.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() is null)
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Public instance + static methods on <paramref name="type"/> (no special names).</summary>
    public static IReadOnlyList<MethodInfo> PublicMethods(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        return type.GetMethods(flags)
            .Where(m => !m.IsSpecialName)
            .OrderBy(m => m.Name, StringComparer.Ordinal)
            .ThenBy(m => m.ToString(), StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Flatten public methods across exported types.</summary>
    public static IReadOnlyList<(Type Type, MethodInfo Method)> PublicMethods(Assembly assembly) =>
        PublicTypes(assembly)
            .SelectMany(t => PublicMethods(t).Select(m => (t, m)))
            .ToList();

    /// <summary>
    /// Invoke parameterless public static methods and public parameterless constructors
    /// where safe (no by-ref args). Returns failures without throwing.
    /// </summary>
    public static IReadOnlyList<string> SmokeInvokeParameterless(Assembly assembly)
    {
        var failures = new List<string>();
        foreach (var type in PublicTypes(assembly))
        {
            if (type.IsAbstract || type.IsGenericTypeDefinition)
                continue;

            try
            {
                if (type.GetConstructor(Type.EmptyTypes) is { } ctor)
                    _ = ctor.Invoke([]);
            }
            catch (Exception ex)
            {
                failures.Add($"{type.FullName}..ctor(): {ex.GetBaseException().Message}");
            }

            foreach (var method in PublicMethods(type))
            {
                if (!method.IsStatic || method.GetParameters().Length != 0 || method.ContainsGenericParameters)
                    continue;
                try
                {
                    _ = method.Invoke(null, null);
                }
                catch (Exception ex)
                {
                    failures.Add($"{type.FullName}.{method.Name}(): {ex.GetBaseException().Message}");
                }
            }
        }

        return failures;
    }
}
