using System;

namespace Kwerty.DviZe.Resilience;

public interface IDelayGenerator
{
    public bool TryNext(int index, out TimeSpan delay);
}