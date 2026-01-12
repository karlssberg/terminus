using Microsoft.CodeAnalysis.CSharp.Syntax;
using Terminus.Generator.Builders.Strategies;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders.Method;

/// <summary>
/// Orchestrates the building of complete method declarations (signature + body).
/// </summary>
internal sealed class MethodBuilder(IServiceResolutionStrategy serviceResolution)
{
    private readonly MethodBodyBuilder _bodyBuilder = new(serviceResolution);

    /// <summary>
    /// Builds a complete implementation method declaration (signature + body).
    /// </summary>
    public MethodDeclarationSyntax BuildImplementationMethod(
        FacadeInterfaceInfo facadeInfo,
        AggregatedMethodGroup methodGroup)
    {
        var methodStub = MethodSignatureBuilder.BuildImplementationMethodStub(facadeInfo, methodGroup);
        var body = _bodyBuilder.BuildMethodBody(facadeInfo, methodGroup);

        return methodStub.WithBody(Block(body));
    }
}
