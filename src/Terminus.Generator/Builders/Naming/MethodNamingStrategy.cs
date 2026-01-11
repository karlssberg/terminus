namespace Terminus.Generator.Builders.Naming;

/// <summary>
/// Determines the method name for a facade method based on the facade configuration and method return type.
/// </summary>
internal static class MethodNamingStrategy
{
    /// <summary>
    /// Gets the appropriate method name based on facade configuration and return type.
    /// </summary>
    public static string GetMethodName(FacadeInterfaceInfo facadeInfo, CandidateMethodInfo candidate)
    {
        return candidate.ReturnTypeKind switch
        {
            ReturnTypeKind.Void => facadeInfo.CommandName ?? candidate.MethodSymbol.Name,
            ReturnTypeKind.Result => facadeInfo.QueryName ?? candidate.MethodSymbol.Name,
            ReturnTypeKind.Task => facadeInfo.AsyncCommandName ?? candidate.MethodSymbol.Name,
            ReturnTypeKind.ValueTask => facadeInfo.AsyncCommandName ?? candidate.MethodSymbol.Name,
            ReturnTypeKind.TaskWithResult => facadeInfo.AsyncQueryName ?? candidate.MethodSymbol.Name,
            ReturnTypeKind.ValueTaskWithResult => facadeInfo.AsyncQueryName ?? candidate.MethodSymbol.Name,
            ReturnTypeKind.AsyncEnumerable => facadeInfo.AsyncStreamName ?? candidate.MethodSymbol.Name,
            _ => candidate.MethodSymbol.Name
        };
    }
}
