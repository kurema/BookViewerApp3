using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Windows.Media.ContentRestrictions;

#nullable enable

namespace BookViewerApp.Managers;
public static partial class ExtensionAdBlockerManager
{
    public class Whitelist : IList<string>
    {
        List<string> content = new();
        List<string> contentForSearch = new();

        public string this[int index] { get => ((IList<string>)content)[index]; set => ((IList<string>)content)[index] = value; }

        public int Count => ((ICollection<string>)content).Count;

        public bool IsReadOnly => ((ICollection<string>)content).IsReadOnly;

        public void AddRange(IEnumerable<string> collection)
        {
            content.AddRange(collection);
            bool updated = false;
            foreach (var item in collection)
            {
                if (IsValidEntry(item))
                {
                    contentForSearch.Add(item.ToUpperInvariant());
                    updated = true;
                }
            }
            if (updated) contentForSearch.Sort();
        }

        public void Add(string item)
        {
            ((ICollection<string>)content).Add(item);
            AddToSearch(item);
        }

        private void AddToSearch(string item)
        {
            item = item.ToUpperInvariant();
            if (IsValidEntry(item))
            {
                ((ICollection<string>)contentForSearch).Add(item);
                contentForSearch.Sort();
            }
        }

        private static bool IsValidEntry(string item)
        {
            return !item.StartsWith("#") && !string.IsNullOrWhiteSpace(item);
        }

        public void Clear()
        {
            ((ICollection<string>)content).Clear();
            ((ICollection<string>)contentForSearch).Clear();
        }


        public void CopyTo(string[] array, int arrayIndex)
        {
            ((ICollection<string>)content).CopyTo(array, arrayIndex);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return ((IEnumerable<string>)content).GetEnumerator();
        }

        public int IndexOf(string item)
        {
            return ((IList<string>)content).IndexOf(item);
        }

        public void Insert(int index, string item)
        {
            ((IList<string>)content).Insert(index, item);
            AddToSearch(item);
        }

        /// <summary>
        /// Remove with IgnoreCase enabled.
        /// </summary>
        /// <param name="item">Domain name</param>
        /// <returns>Result</returns>
        public bool Remove(string item)
        {
            ((ICollection<string>)contentForSearch).Remove(item.ToUpperInvariant());

            bool result = false;
            foreach (var entry in content.Where(a => a.Equals(item, System.StringComparison.CurrentCultureIgnoreCase)).ToArray())
            {
                result |= content.Remove(entry);
            }
            return result;
        }

        public void RemoveAt(int index)
        {
            var item = content.ElementAt(index);
            contentForSearch.Remove(item.ToUpperInvariant());
            ((IList<string>)content).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)content).GetEnumerator();
        }

        public bool Contains(string item)
        {
            return ((ICollection<string>)content).Contains(item);
        }

        public bool ContainsDomain(string item)
        {
            return contentForSearch.BinarySearch(item.ToUpperInvariant()) >= 0;
        }

        public bool ContainsWildcard(string item)
        {
            var upper = item.ToUpperInvariant();// This operation may be called twice. It's not smart in performance but ignorable.
            if (ContainsDomain(upper)) return true;
            foreach (var entry in GetWildcardCandidates(upper))
            {
                if (ContainsDomain(entry)) return true;
            }
            return false;
        }

        public bool RemoveWildcard(string item)
        {
            bool result = false;
            var wc = GetWildcardCandidates(item);
            foreach (var wildcard in wc)
            {
                contentForSearch.Remove(wildcard.ToUpperInvariant());
            }

            for (int i=0;i<content.Count;i++)
            {
                foreach (var wildcard in wc)
                {
                    if(content[i].Equals(wildcard, System.StringComparison.CurrentCultureIgnoreCase))
                    {
                        result = true;
                        content[i] = "#" + content[i];
                    }
                }
            }

            return result;
        }

        public static IEnumerable<string> GetWildcardCandidates(string domain)
        {
            int lastIndex = -1;
            while (true)
            {
                lastIndex = domain.IndexOf('.', lastIndex + 1);
                if (lastIndex < 0) break;
                yield return "*" + domain.Substring(lastIndex);
            }
        }
    }
}
