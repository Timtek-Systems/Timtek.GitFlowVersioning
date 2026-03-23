namespace Timtek.GitFlowVersion.Scenarios;

/// <summary>
/// Represents a single fluent builder operation in a reconstructed repository chain
/// (e.g. <c>.WithInitialCommit()</c>, <c>.WithTag("1.0.0")</c>).
/// </summary>
public sealed class BuilderStep
{
    /// <summary>Gets the builder method name (e.g. "WithInitialCommit").</summary>
    public string MethodName { get; }

    /// <summary>Gets the string argument, if any (tag name or branch name).</summary>
    public string? StringArgument { get; }

    /// <summary>Gets the integer argument, if any (commit count).</summary>
    public int? IntArgument { get; }

    private BuilderStep(string methodName, string? stringArgument = null, int? intArgument = null)
    {
        MethodName = methodName;
        StringArgument = stringArgument;
        IntArgument = intArgument;
    }

    /// <summary>Creates a step representing <c>.WithInitialCommit()</c>.</summary>
    public static BuilderStep InitialCommit() => new BuilderStep("WithInitialCommit");

    /// <summary>Creates a step representing <c>.WithTag("name")</c>.</summary>
    public static BuilderStep Tag(string name) => new BuilderStep("WithTag", name);

    /// <summary>Creates a step representing <c>.WithBranch("name")</c>.</summary>
    public static BuilderStep Branch(string name) => new BuilderStep("WithBranch", name);

    /// <summary>Creates a step representing <c>.OnBranch("name")</c>.</summary>
    public static BuilderStep Checkout(string name) => new BuilderStep("OnBranch", name);

    /// <summary>Creates a step representing <c>.MergeFrom("name")</c>.</summary>
    public static BuilderStep Merge(string name) => new BuilderStep("MergeFrom", name);

    /// <summary>Creates a step representing <c>.WithCommits(count)</c>.</summary>
    public static BuilderStep Commits(int count) => new BuilderStep("WithCommits", intArgument: count);

    /// <summary>Formats this step as a C# method call fragment.</summary>
    public string ToCode()
    {
        if (StringArgument != null)
            return $".{MethodName}(\"{StringArgument}\")";
        if (IntArgument.HasValue)
            return $".{MethodName}({IntArgument.Value})";
        return $".{MethodName}()";
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is BuilderStep other
        && MethodName == other.MethodName
        && StringArgument == other.StringArgument
        && IntArgument == other.IntArgument;

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = MethodName.GetHashCode();
            if (StringArgument != null)
                hash = hash * 31 + StringArgument.GetHashCode();
            if (IntArgument.HasValue)
                hash = hash * 31 + IntArgument.Value;
            return hash;
        }
    }

    /// <inheritdoc />
    public override string ToString() => ToCode();
}
