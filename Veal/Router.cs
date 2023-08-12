using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Veal
{
    public delegate void RouteAction(HttpListenerContext ctx, Dictionary<string, string> data);


    // Each new route is assigned a key from permutations of `KeyBase` ("123456") and is stored in 
    // `_routes` dictionary. Router implementation builds a composite regex from all routes 
    // patterns that looks like
    //    route_pattern1 | route_pattern2 | route_pattern3 | route_pattern4 | ...
    // where `route_patternN` is prefixed with it's key pattern that looks like
    //    ^(?<__c1__>1)(?<__c5__>2)(?<__c3__>3)(?<__c2__>4)(?<__c4__>5)(?<__c6__>6)
    // These key patterns always match `KeyBase` ("123456") but in different named captures, so in the 
    // sample key pattern above when matched against "123456/local/path" the `__c1__` to `__c6__` 
    // named captures will concatenate to "143526" for currently matched route key. The corresponding 
    // entry in `_routes` has `GroupStart` to `GroupEnd` that are used to extract handler data 
    // dictionary from the composite regex anonymous captures.
    internal class Router : IDisposable
    {
        private static readonly string KeyBase = "123456";
        private static readonly Regex RoutePattern = new Regex(@"(/(({(?<data>[^}/:]+)(:(?<type>[^}/]+))?}?)|(?<static>[^/]+))|\*)",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private class RouteEntry
        {
            public string Pattern { get; set; }
            public int GroupStart { get; set; }
            public int GroupEnd { get; set; }
            public RouteAction Handler { get; set; }
        }

        private Dictionary<string, RouteEntry> _routes = new Dictionary<string, RouteEntry>();
        private IEnumerator<IEnumerable<char>> _permEnum = GetPermutations(KeyBase.ToCharArray(), KeyBase.Length).GetEnumerator();
        private string[] _groupNames = new string[32];
        private Regex _pathParser;

        public void Add(string route, RouteAction handler)
        {
            // for each "{key:type}" check regex pattern in `type` and raise `ArgumentException` on failure
            RoutePattern.Replace(route, m =>
            {
                if (string.IsNullOrEmpty(m.Groups["static"].Value) && !string.IsNullOrEmpty(m.Groups["data"].Value)
                        && !string.IsNullOrEmpty(m.Groups["type"].Value))
                    Regex.Match("", m.Groups["type"].Value);
                return null;
            });
            _permEnum.MoveNext();
            _routes.Add(string.Join(null, _permEnum.Current), new RouteEntry { Pattern = route, Handler = handler });
            _pathParser = null;
        }

        public bool TryGetValue(string localPath, out RouteAction handler, out Dictionary<string, string> data)
        {
            handler = null;
            data = null;
            if (_pathParser == null)
                _pathParser = RebuildParser();
            var match = _pathParser.Match(KeyBase + localPath);
            if (match.Success)
            {
                string routeKey = null;
                for (int idx = 1; idx <= KeyBase.Length; idx++)
                    routeKey += match.Groups[$"__c{idx}__"].Value;
                var entry = _routes[routeKey];
                handler = entry.Handler;
                if (entry.GroupStart < entry.GroupEnd)
                    data = new Dictionary<string, string>();
                for (var groupIdx = entry.GroupStart; groupIdx < entry.GroupEnd; groupIdx++)
                    data[_groupNames[groupIdx]] = match.Groups[groupIdx].Value;
            }
            return match.Success;
        }

        private Regex RebuildParser()
        {
            string[] rev = new string[KeyBase.Length];
            var sb = new StringBuilder();
            int groupIdx = 1;

            foreach (string key in _routes.Keys)
            {
                var entry = _routes[key];
                entry.GroupStart = groupIdx;
                int el = 1;
                foreach (char c in key.ToCharArray())
                    rev[c - '1'] = $"(?<__c{el++}__>{c})";
                sb.AppendLine((sb.Length > 0 ? "|" : null) + "^" + string.Join(null, rev) +
                    RoutePattern.Replace(entry.Pattern, m =>
                    {
                        string str = m.Groups["static"].Value;
                        if (!string.IsNullOrEmpty(str))
                            return "/" + Regex.Escape(str);
                        str = m.Groups["data"].Value;
                        if (!string.IsNullOrEmpty(str))
                        {
                            if (groupIdx >= _groupNames.Length)
                                Array.Resize(ref _groupNames, _groupNames.Length * 2);
                            _groupNames[groupIdx++] = str;
                            str = m.Groups["type"].Value;
                            return $"/({(string.IsNullOrEmpty(str) ? "[^/]*" : str)})";
                        }
                        return Regex.Escape(m.Groups[0].Value);
                    }));
                entry.GroupEnd = groupIdx;
            }
            return new Regex(sb.ToString(), RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        }

        public void Dispose()
        {
            _permEnum?.Dispose();
            _permEnum = null;
        }

        private static IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> list, int length)
        {
            if (length == 1) return list.Select(t => new T[] { t });
            return GetPermutations(list, length - 1).SelectMany(t => list
                .Where(o => !t.Contains(o)), (t1, t2) => t1.Concat(new T[] { t2 }));
        }
    }
}
