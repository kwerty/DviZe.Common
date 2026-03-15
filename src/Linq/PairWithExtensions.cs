using System;
using System.Collections.Generic;
using System.Linq;

namespace Kwerty.DviZe.Linq;

public static class PairWithExtensions
{
    public static IEnumerable<Pair<TLeft, TRight>> PairWith<TLeft, TRight, TKey>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKeySelector, Func<TRight, TKey> rightKeySelector)
        => PairWith(left, right, (l, r) => leftKeySelector(l).Equals(rightKeySelector(r)));

    public static IEnumerable<Pair<TLeft, TRight>> PairWith<TLeft, TRight>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TRight, bool> comparer)
    {
        var remainingRight = right.ToList();

        foreach (var leftItem in left)
        {
            var didMatch = false;

            foreach (var rightItem in remainingRight)
            {
                if (comparer(leftItem, rightItem))
                {
                    didMatch = true;
                    remainingRight.Remove(rightItem);

                    yield return new Pair<TLeft, TRight>
                    {
                        IsFullPair = true,
                        Left = leftItem,
                        Right = rightItem,
                    };
                    break;
                }
            }

            if (!didMatch)
            {
                yield return new Pair<TLeft, TRight>
                {
                    Left = leftItem,
                    IsRightEmpty = true,
                };
            }
        }

        foreach (var item in remainingRight)
        {
            yield return new Pair<TLeft, TRight>
            {
                IsLeftEmpty = true,
                Right = item,
            };
        }
    }
}