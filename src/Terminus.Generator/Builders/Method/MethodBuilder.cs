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
    /// Builds an interface method declaration (signature only).
    /// </summary>
    public MethodDeclarationSyntax BuildInterfaceMethod(
        FacadeInterfaceInfo facadeInfo,
        CandidateMethodInfo methodInfo)
    {
        return MethodSignatureBuilder.BuildInterfaceMethod(facadeInfo, methodInfo);
    }

    /// <summary>
    /// Builds a complete implementation method declaration (signature + body).
    /// </summary>
    public MethodDeclarationSyntax BuildImplementationMethod(
        FacadeInterfaceInfo facadeInfo,
        CandidateMethodInfo methodInfo)
    {
        var methodStub = MethodSignatureBuilder.BuildImplementationMethodStub(facadeInfo, methodInfo);
        var body = _bodyBuilder.BuildMethodBody(facadeInfo, methodInfo);

        return methodStub.WithBody(Block(body));
    }
}
