# PairWith

An `IEnumerable<T>` extension that pairs elements from two sequences by key or predicate, yielding `Pair<TLeft, TRight>` results. Each left element is paired with at most one right element, and unmatched elements from either side are yielded as half-pairs.

```csharp
public IEnumerable<Pair<TLeft, TRight>> PairWith<TLeft, TRight, TKey>(IEnumerable<TRight> right, Func<TLeft, TKey> leftKeySelector, Func<TRight, TKey> rightKeySelector);
```

Matches elements from the left and right sequences by comparing keys extracted via `leftKeySelector` and `rightKeySelector`.

```csharp
public IEnumerable<Pair<TLeft, TRight>> PairWith<TLeft, TRight>(IEnumerable<TRight> right, Func<TLeft, TRight, bool> comparer);
```

Matches elements from the left and right sequences using a custom `comparer` predicate.

## Examples

The setup.

```csharp
record Student(int Id, string Name);

record Score(int StudentId, int Value);

List<Student> students =
[
    new Student(1, "Alice"),
    new Student(2, "Bob"),
];

List<Score> scores =
[
    new Score(1, 95),
    new Score(3, 80),
];
```

Pair students with scores.

```csharp
var pairs = students.PairWith(scores, st => st.Id, sc => sc.StudentId);
```

Inspect each result.

```csharp
foreach (var pair in pairs)
{
    if (pair.IsFullPair)
    {
        // Student (pair.Left) and score (pair.Right) are both present.
    }
    else if (pair.IsLeftEmpty)
    {
        // Only the score (pair.Right) is present.
    }
    else if (pair.IsRightEmpty)
    {
        // Only the student (pair.Left) is present.
    }
}
```
