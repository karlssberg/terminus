namespace Terminus.Example.CleanArchitecture.Domain.Mediation;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class DomainOperationAttribute : Attribute;