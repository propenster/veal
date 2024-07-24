using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Veal
{
    /// <summary>
    /// holds data and states passed from the action to the STATIC view file e.g HTML
    /// </summary>
    public class ViewContext : IDictionary<string, object>
    {
        private readonly IDictionary<string, object> _viewContextData;

        public ViewContext()
        {
            _viewContextData = new Dictionary<string, object>();
        }
        //public Dictionary<string, object> _viewContextData { get; set; } = new Dictionary<string, object>();
        public object this[string index]
        {
            get
            {
                _viewContextData.TryGetValue(index, out var result);
                return result;
            }
            set
            {
                _viewContextData[index] = value;
            }
        }
        public int Count
        {
            get { return _viewContextData.Count; }
        }
        public bool IsReadOnly
        {
            get { return _viewContextData.IsReadOnly; }
        }
        public ICollection<string> Keys
        {
            get { return _viewContextData.Keys; }
        }
        public ICollection<object> Values
        {
            get { return _viewContextData.Values; }
        }
        public void Add(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            _viewContextData.Add(key, value);
        }
        public bool ContainsKey(string key)
        {
            return _viewContextData.ContainsKey(key);
        }
        public bool Remove(string key)
        {
            return _viewContextData.Remove(key);
        }
        public bool TryGetValue(string key, out object value)
        {
            return _viewContextData.TryGetValue(key, out value);
        }
        public void Add(KeyValuePair<string, object> item)
        {
            _viewContextData.Add(item);
        }
        public void Clear()
        {
            _viewContextData.Clear();
        }
        public bool Contains(KeyValuePair<string, object> item)
        {
            return _viewContextData.Contains(item);
        }
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            _viewContextData.CopyTo(array, arrayIndex);
        }
        public bool Remove(KeyValuePair<string, object> item)
        {
            return _viewContextData.Remove(item);
        }
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _viewContextData.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _viewContextData.GetEnumerator();
        }
    }
}
