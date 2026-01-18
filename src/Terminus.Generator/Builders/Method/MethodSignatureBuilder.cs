using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Terminus.Generator.Builders.Documentation;
using Terminus.Generator.Builders.Naming;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Terminus.Generator.Builders.Method;

/// <summary>
/// Builds method signature (return type, name, parameters) for facade methods.
/// </summary>
internal sealed class MethodSignatureBuilder
{
    /// <summary>
    /// Builds a method declaration with signature only (no body).
    /// </summary>
    public static MethodDeclarationSyntax BuildInterfaceMethod(
        FacadeInterfaceInfo facadeInfo,
        AggregatedMethodGroup methodGroup)
    {
        var methodInfo = methodGroup.PrimaryMethod;
        var returnTypeSyntax = BuildReturnType(facadeInfo, methodGroup);
        var methodName = MethodNamingStrategy.GetMethodName(facadeInfo, methodInfo);
        var parameterList = BuildParameterList(methodInfo);
        var typeParameterList = BuildTypeParameterList(methodInfo);
        var typeParameterConstraintList = BuildTypeParameterConstraintList(methodInfo);
        var documentation = DocumentationBuilder.BuildMethodDocumentation(facadeInfo, methodGroup);

        return MethodDeclaration(returnTypeSyntax, Identifier(methodName))
            .WithLeadingTrivia(documentation)
            .WithTypeParameterList(typeParameterList)
            .WithParameterList(parameterList)
            .WithConstraintClauses(typeParameterConstraintList)
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }

    /// <summary>
    /// Builds a method declaration stub for implementation (signature + explicit interface + modifiers).
    /// </summary>
    public static MethodDeclarationSyntax BuildImplementationMethodStub(
        FacadeInterfaceInfo facadeInfo,
        AggregatedMethodGroup methodGroup)
    {
        var methodInfo = methodGroup.PrimaryMethod;
        var returnTypeSyntax = BuildReturnType(facadeInfo, methodGroup);
        var methodName = MethodNamingStrategy.GetMethodName(facadeInfo, methodInfo);
        var parameterList = BuildParameterList(methodInfo);
        var typeParameterList = BuildTypeParameterList(methodInfo);

        // Use explicit interface implementation
        var interfaceName = facadeInfo.InterfaceSymbol
            .ToDisplayString(FullyQualifiedFormat);
        var explicitInterfaceSpecifier = ExplicitInterfaceSpecifier(ParseName(interfaceName));

        var method = MethodDeclaration(returnTypeSyntax, Identifier(methodName))
            .WithExplicitInterfaceSpecifier(explicitInterfaceSpecifier)
            .WithTypeParameterList(typeParameterList)
            .WithParameterList(parameterList);

        // Maybe use async keyword
        if (ShouldUseAsyncKeyword(facadeInfo, methodInfo, methodGroup.RequiresAggregation))
        {
            method = method.AddModifiers(Token(SyntaxKind.AsyncKeyword));
        }

        return method;
    }

    private static bool ShouldUseAsyncKeyword(FacadeInterfaceInfo facadeInfo, CandidateMethodInfo methodInfo, bool requiresAggregation)
    {
        // When metadata mode is active, no async keyword needed (returns IEnumerable synchronously with lazy delegates)
        if (facadeInfo.Features.IncludeAttributeMetadata)
            return false;

        // Add async modifier when returning Task/ValueTask/Task<T>/ValueTask<T> or when generating an async iterator
        // for IAsyncEnumerable in a scoped facade (we create an async scope and yield items).
        if (methodInfo.ReturnTypeKind is ReturnTypeKind.AsyncEnumerable && facadeInfo.Features.IsScoped)
            return true;

        // For aggregated methods with async results, we generate IAsyncEnumerable which needs async
        if (requiresAggregation && (methodInfo.ReturnTypeKind is ReturnTypeKind.TaskWithResult or ReturnTypeKind.ValueTaskWithResult))
            return true;

        return methodInfo.ReturnTypeKind is ReturnTypeKind.Task
                                         or ReturnTypeKind.TaskWithResult
                                         or ReturnTypeKind.ValueTask
                                         or ReturnTypeKind.ValueTaskWithResult;
    }

    private static TypeSyntax BuildReturnType(FacadeInterfaceInfo facadeInfo, AggregatedMethodGroup methodGroup)
    {
        var methodInfo = methodGroup.PrimaryMethod;
        var requiresAggregation = methodGroup.RequiresAggregation;
        var includeMetadata = facadeInfo.Features.IncludeAttributeMetadata;
        var commonAttributeType = methodGroup.CommonAttributeType;

        // Handle metadata mode with lazy execution - works for single or multiple methods
        if (includeMetadata && commonAttributeType != null)
        {
            return BuildLazyMetadataReturnType(methodInfo, commonAttributeType);
        }

        // For non-aggregated methods (without metadata), use original return type
        if (!requiresAggregation)
        {
            return methodInfo.MethodSymbol.ReturnsVoid
                ? PredefinedType(Token(SyntaxKind.VoidKeyword))
                : ParseTypeName(methodInfo.MethodSymbol.ReturnType
                    .ToDisplayString(FullyQualifiedFormat));
        }

        // For aggregated methods (without metadata), transform return types:
        // - void stays void (all handlers execute, no return)
        // - T becomes IEnumerable<T>
        // - Task<T> becomes IAsyncEnumerable<T>
        // - ValueTask<T> becomes IAsyncEnumerable<T>
        if (methodInfo.MethodSymbol.ReturnsVoid)
        {
            return PredefinedType(Token(SyntaxKind.VoidKeyword));
        }

        var returnType = methodInfo.MethodSymbol.ReturnType;

        return methodInfo.ReturnTypeKind switch
        {
            // T → IEnumerable<T>
            ReturnTypeKind.Result => ParseTypeName(
                $"global::System.Collections.Generic.IEnumerable<{returnType.ToDisplayString(FullyQualifiedFormat)}>"),

            // Task<T> or ValueTask<T> → IAsyncEnumerable<T>
            ReturnTypeKind.TaskWithResult or ReturnTypeKind.ValueTaskWithResult => ParseTypeName(
                $"global::System.Collections.Generic.IAsyncEnumerable<{((INamedTypeSymbol)returnType).TypeArguments[0].ToDisplayString(FullyQualifiedFormat)}>"),

            // Task, ValueTask, void - keep as is
            _ => ParseTypeName(returnType.ToDisplayString(FullyQualifiedFormat))
        };
    }

    /// <summary>
    /// Builds return type for metadata mode: IEnumerable&lt;(TAttribute Attribute, Func/Action Handler)&gt;
    /// </summary>
    private static TypeSyntax BuildLazyMetadataReturnType(CandidateMethodInfo methodInfo, INamedTypeSymbol attributeType)
    {
        var attributeTypeName = attributeType.ToDisplayString(FullyQualifiedFormat);
        var returnType = methodInfo.MethodSymbol.ReturnType;

        var delegateType = methodInfo.ReturnTypeKind switch
        {
            ReturnTypeKind.Void =>
                "global::System.Action",
            ReturnTypeKind.Result =>
                $"global::System.Func<{returnType.ToDisplayString(FullyQualifiedFormat)}>",
            ReturnTypeKind.Task =>
                "global::System.Func<global::System.Threading.Tasks.Task>",
            ReturnTypeKind.ValueTask =>
                "global::System.Func<global::System.Threading.Tasks.ValueTask>",
            ReturnTypeKind.TaskWithResult =>
                $"global::System.Func<global::System.Threading.Tasks.Task<{((INamedTypeSymbol)returnType).TypeArguments[0].ToDisplayString(FullyQualifiedFormat)}>>",
            ReturnTypeKind.ValueTaskWithResult =>
                $"global::System.Func<global::System.Threading.Tasks.ValueTask<{((INamedTypeSymbol)returnType).TypeArguments[0].ToDisplayString(FullyQualifiedFormat)}>>",
            ReturnTypeKind.AsyncEnumerable =>
                $"global::System.Func<{returnType.ToDisplayString(FullyQualifiedFormat)}>",
            _ =>
                $"global::System.Func<{returnType.ToDisplayString(FullyQualifiedFormat)}>"
        };

        return ParseTypeName(
            $"global::System.Collections.Generic.IEnumerable<({attributeTypeName} Attribute, {delegateType} Handler)>");
    }

    private static TypeSyntax BuildTupleEnumerableType(INamedTypeSymbol attributeType, ITypeSymbol resultType)
    {
        var attributeTypeName = attributeType.ToDisplayString(FullyQualifiedFormat);
        var resultTypeName = resultType.ToDisplayString(FullyQualifiedFormat);

        return ParseTypeName(
            $"global::System.Collections.Generic.IEnumerable<({attributeTypeName} Attribute, {resultTypeName} Result)>");
    }

    private static TypeSyntax BuildTupleAsyncEnumerableType(INamedTypeSymbol attributeType, ITypeSymbol resultType)
    {
        var attributeTypeName = attributeType.ToDisplayString(FullyQualifiedFormat);
        var resultTypeName = resultType.ToDisplayString(FullyQualifiedFormat);

        return ParseTypeName(
            $"global::System.Collections.Generic.IAsyncEnumerable<({attributeTypeName} Attribute, {resultTypeName} Result)>");
    }

    private static ParameterListSyntax BuildParameterList(CandidateMethodInfo methodInfo)
    {
        return ParameterList(SeparatedList(
            methodInfo.MethodSymbol.Parameters.Select(p =>
            {
                var parameter = Parameter(Identifier(p.Name.EscapeIdentifier()))
                    .WithType(ParseTypeName(p.Type
                        .ToDisplayString(FullyQualifiedFormat)));

                if (p.RefKind == RefKind.In)
                {
                    parameter = parameter.AddModifiers(Token(SyntaxKind.InKeyword));
                }

                if (p.HasExplicitDefaultValue)
                {
                    var defaultValue = p.ExplicitDefaultValue switch
                    {
                        string s => LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(s)),
                        bool b => LiteralExpression(b ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression),
                        null => LiteralExpression(SyntaxKind.NullLiteralExpression),
                        _ => ParseExpression(p.ExplicitDefaultValue.ToString() ?? "default")
                    };

                    parameter = parameter.WithDefault(EqualsValueClause(defaultValue));
                }

                return parameter;
            })));
    }

    private static TypeParameterListSyntax? BuildTypeParameterList(CandidateMethodInfo methodInfo)
    {
        if (!methodInfo.MethodSymbol.IsGenericMethod)
            return null;

        return TypeParameterList(SeparatedList(
            methodInfo.MethodSymbol.TypeParameters.Select(tp =>
                TypeParameter(Identifier(tp.Name.EscapeIdentifier())))));
    }

    private static SyntaxList<TypeParameterConstraintClauseSyntax> BuildTypeParameterConstraintList(CandidateMethodInfo methodInfo)
    {
        if (!methodInfo.MethodSymbol.IsGenericMethod)
            return default;

        var clauses = methodInfo.MethodSymbol.TypeParameters
            .Select(BuildTypeParameterConstraintClause)
            .Where(c => c != null)
            .Cast<TypeParameterConstraintClauseSyntax>();

        return List(clauses);
    }

    private static TypeParameterConstraintClauseSyntax? BuildTypeParameterConstraintClause(ITypeParameterSymbol tp)
    {
        var constraints = new List<TypeParameterConstraintSyntax>();

        if (tp.HasReferenceTypeConstraint)
            constraints.Add(ClassOrStructConstraint(SyntaxKind.ClassConstraint));
        else if (tp.HasValueTypeConstraint)
            constraints.Add(ClassOrStructConstraint(SyntaxKind.StructConstraint));

        constraints.AddRange(tp.ConstraintTypes
            .Select(typeConstraint => 
                TypeConstraint(ParseTypeName(typeConstraint.ToDisplayString(FullyQualifiedFormat)))));

        if (tp.HasConstructorConstraint)
            constraints.Add(ConstructorConstraint());

        if (constraints.Count == 0)
            return null;

        return TypeParameterConstraintClause(IdentifierName(tp.Name.EscapeIdentifier()))
            .WithConstraints(SeparatedList(constraints));
    }
}
