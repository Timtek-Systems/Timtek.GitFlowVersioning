namespace Timtek.GitFlowVersion.Versioning;

/// <summary>Classifies a git branch according to GitFlow conventions.</summary>
public enum BranchType
{
    /// <summary>The main or master production branch.</summary>
    Main,
    /// <summary>The develop integration branch.</summary>
    Develop,
    /// <summary>A release/* branch.</summary>
    Release,
    /// <summary>A hotfix/* branch.</summary>
    Hotfix,
    /// <summary>Any other branch (feature/*, bugfix/*, etc.).</summary>
    Other
}
