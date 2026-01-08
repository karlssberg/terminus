using Microsoft.CodeAnalysis.CSharp.Syntax;
using Terminus.Generator.Builders.Strategies;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminus.Generator.Builders.Method;

/// <summary>
/// Orchestrates the building of complete method declarations (signature + body).
/// </summary>
internal sealed class MethodBuilder
{
    private readonly MethodSignatureBuilder _signatureBuilder;
    private readonly MethodBodyBuilder _bodyBuilder;

    public MethodBuilder(IServiceResolutionStrategy serviceResolution)
    {
        _signatureBuilder = new MethodSignatureBuilder();
        _bodyBuilder = new MethodBodyBuilder(serviceResolution);
    }

    /// <summary>
    /// Builds an interface method declaration (signature only).
    /// </summary>
    public MethodDeclarationSyntax BuildInterfaceMethod(
        FacadeInterfaceInfo facadeInfo,
        CandidateMethodInfo methodInfo)
    {
        return _signatureBuilder.BuildInterfaceMethod(facadeInfo, methodInfo);
    }

    /// <summary>
    /// Builds a complete implementation method declaration (signature + body).
    /// </summary>
    public MethodDeclarationSyntax BuildImplementationMethod(
        FacadeInterfaceInfo facadeInfo,
        CandidateMethodInfo methodInfo)
    {
        var methodStub = _signatureBuilder.BuildImplementationMethodStub(facadeInfo, methodInfo);
        var body = _bodyBuilder.BuildMethodBody(facadeInfo, methodInfo);

        return methodStub.WithBody(Block(body));
    }
}
