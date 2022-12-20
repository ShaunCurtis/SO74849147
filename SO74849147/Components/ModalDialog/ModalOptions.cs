using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Blazr.UI;

public sealed class ModalOptions : IEnumerable<KeyValuePair<string, object>>
{
    public string Width { get; set; } = "50%";

    public Dictionary<string, object> ControlParameters { get; } = new Dictionary<string, object>();

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        foreach (var item in ControlParameters)
            yield return item;
    }

    IEnumerator IEnumerable.GetEnumerator()
        => this.GetEnumerator();

    public T? Get<T>(string key)
    {
        if (this.ControlParameters.ContainsKey(key))
        {
            if (this.ControlParameters[key] is T t) return t;
        }
        return default;
    }

    public bool TryGet<T>(string key, [NotNullWhen(true)] out T? value)
    {
        value = default;
        if (this.ControlParameters.ContainsKey(key))
        {
            if (this.ControlParameters[key] is T t)
            {
                value = t;
                return true;
            }
        }
        return false;
    }

    public bool Set(string key, object value)
    {
        if (this.ControlParameters.ContainsKey(key))
        {
            this.ControlParameters[key] = value;
            return false;
        }
        this.ControlParameters.Add(key, value);
        return true;
    }
}

