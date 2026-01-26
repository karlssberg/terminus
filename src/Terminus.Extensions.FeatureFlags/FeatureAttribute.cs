namespace Terminus.Extensions.FeatureFlags;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class FeatureAttribute(string featureName) : Attribute
{
    public string FeatureName { get; } = featureName;
}