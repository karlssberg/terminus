namespace Terminus.Example.CleanArchitecture.Domain.Mediation.Gateways;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface)]
public class GatewayAttribute : Attribute;