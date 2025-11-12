namespace Terminus;

public interface IEntryPointRouter<TFacade>
{
    bool IsMatch(IEntryPointDescriptor ep, ParameterBindingContext context);
}
