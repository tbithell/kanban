using System.Numerics;
using System.Runtime.CompilerServices;

namespace Kanban.Domain;

public static class Verify
{
    public static ParameterVerifier<T> That<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string paramName = "")
        => new(value, paramName);
}

public sealed class ParameterVerifier<T>
{
    internal readonly T Value;
    internal readonly string ParamName;

    internal ParameterVerifier(T value, string paramName)
    {
        Value = value;
        ParamName = paramName;
    }

    public ParameterVerifier<T> IsNotNull()
    {
        if (Value is null)
            throw new ArgumentNullException(ParamName);
        return this;
    }

    public ParameterVerifier<T> IsNotDefault()
    {
        if (EqualityComparer<T>.Default.Equals(Value, default))
            throw new ArgumentException("Value must not be default.", ParamName);
        return this;
    }
}

public static class StringVerifierExtensions
{
    public static ParameterVerifier<string> IsNotEmpty(this ParameterVerifier<string> verifier)
    {
        if (string.IsNullOrEmpty(verifier.Value))
            throw new ArgumentException("Value must not be empty.", verifier.ParamName);
        return verifier;
    }

    public static ParameterVerifier<string> HasMaxLength(this ParameterVerifier<string> verifier, int max)
    {
        if (verifier.Value is not null && verifier.Value.Length > max)
            throw new ArgumentException($"Value must not exceed {max} characters.", verifier.ParamName);
        return verifier;
    }
}

public static class NumberVerifierExtensions
{
    public static ParameterVerifier<T> IsPositive<T>(this ParameterVerifier<T> verifier)
        where T : INumber<T>
    {
        if (verifier.Value <= T.Zero)
            throw new ArgumentException("Value must be positive (> 0).", verifier.ParamName);
        return verifier;
    }

    public static ParameterVerifier<T> IsNonNegative<T>(this ParameterVerifier<T> verifier)
        where T : INumber<T>
    {
        if (verifier.Value < T.Zero)
            throw new ArgumentException("Value must be non-negative (>= 0).", verifier.ParamName);
        return verifier;
    }
}

public static class ComparableVerifierExtensions
{
    public static ParameterVerifier<T> IsGreaterThan<T>(this ParameterVerifier<T> verifier, T threshold)
        where T : IComparable<T>
    {
        if (verifier.Value.CompareTo(threshold) <= 0)
            throw new ArgumentException($"Value must be greater than {threshold}.", verifier.ParamName);
        return verifier;
    }

    public static ParameterVerifier<T> IsInRange<T>(this ParameterVerifier<T> verifier, T min, T max)
        where T : IComparable<T>
    {
        if (verifier.Value.CompareTo(min) < 0 || verifier.Value.CompareTo(max) > 0)
            throw new ArgumentException($"Value must be in range [{min}, {max}].", verifier.ParamName);
        return verifier;
    }
}

public static class EnumerableVerifierExtensions
{
    public static ParameterVerifier<T> IsNotEmptyCollection<T>(this ParameterVerifier<T> verifier)
        where T : System.Collections.IEnumerable
    {
        var enumerator = verifier.Value?.GetEnumerator();
        if (enumerator is null || !enumerator.MoveNext())
            throw new ArgumentException("Collection must not be empty.", verifier.ParamName);
        return verifier;
    }
}
