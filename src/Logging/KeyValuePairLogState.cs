using System.Collections;
using System.Collections.Generic;

namespace Kwerty.DviZe.Logging;

public class KeyValuePairLogState(List<KeyValuePair<string, object>> pairs = null)
    : IReadOnlyList<KeyValuePair<string, object>>
{
    List<KeyValuePair<string, object>> pairs = pairs;
    bool hasComputed;

    List<KeyValuePair<string, object>> GetKeyValuePairs()
    {
        if (!hasComputed)
        {
            hasComputed = true;
            pairs ??= [];
            OnComputingKeyValuePairs(pairs);
        }
        return pairs;
    }

    IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        => GetKeyValuePairs().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => ((IEnumerable<KeyValuePair<string, object>>)this).GetEnumerator();

    KeyValuePair<string, object> IReadOnlyList<KeyValuePair<string, object>>.this[int index]
        => GetKeyValuePairs()[index];

    int IReadOnlyCollection<KeyValuePair<string, object>>.Count
        => GetKeyValuePairs().Count;

    protected virtual void OnComputingKeyValuePairs(IList<KeyValuePair<string, object>> pairs) { }
}
