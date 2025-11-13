using System;

namespace Terminus;

public interface IMediatorAttribute
{
    Type[] EntryPointAttributes { get; set; }
    Type[] TargetTypes { get; }
}