using System;

namespace Terminus;

public interface IFacadeAttribute
{
    Type[] EntryPointAttributes { get; set; }
    Type[] TargetTypes { get; }
}