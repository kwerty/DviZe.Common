namespace Kwerty.DviZe.Linq;

public sealed class Pair<TLeft, TRight>
{
    internal Pair()
    {
    }

    public bool IsFullPair { get; init; }

    public bool IsLeftEmpty { get; init; }

    public bool IsRightEmpty { get; init; }

    public TLeft Left { get; init; }

    public TRight Right { get; init; }
}