using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders.Strategies;

/// <summary>
/// Resolves instance methods for scoped facades using lazy-initialized service scopes.
/// </summary>
internal sealed class ScopedServiceResolution : IServiceResolutionStrategy
{
    public bool CanResolve(FacadeInterfaceInfo facadeInfo, CandidateMethodInfo methodInfo)
    {
        return facadeInfo.Features.IsScoped && !methodInfo.MethodSymbol.IsStatic;
    }

    public ExpressionSyntax GetServiceExpression(FacadeInterfaceInfo facadeInfo, CandidateMethodInfo methodInfo, bool isAggregation = false)
    {
        var containingType = methodInfo.MethodSymbol.ContainingType;
        var fullyQualifiedTypeName = containingType
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Determine which scope to use based on method return type
        var scopeExpression = methodInfo.ReturnTypeKind is ReturnTypeKind.Task or ReturnTypeKind.TaskWithResult
                                                           or ReturnTypeKind.ValueTask or ReturnTypeKind.ValueTaskWithResult
                                                           or ReturnTypeKind.AsyncEnumerable
            ? "_asyncScope.Value.ServiceProvider"
            : "_syncScope.Value.ServiceProvider";

        // For aggregation on interface/abstract types, use GetServices to get all implementations
        if (isAggregation && (containingType.TypeKind == Microsoft.CodeAnalysis.TypeKind.Interface || containingType.IsAbstract))
        {
            return ParseExpression(
                $"global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetServices<{fullyQualifiedTypeName}>({scopeExpression})");
        }

        return ParseExpression(
            $"global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<{fullyQualifiedTypeName}>({scopeExpression})");
    }
}
