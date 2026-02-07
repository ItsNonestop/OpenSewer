using System;
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace OpenSewer.Utility
{
    public sealed class ReactiveHashSet<T> : IEnumerable<T>
    {
        private readonly BehaviorSubject<HashSet<T>> _subject;

        public ReactiveHashSet()
        {
            _subject = new BehaviorSubject<HashSet<T>>([]);
        }

        public ReactiveHashSet(IEnumerable<T> initial)
        {
            _subject = new BehaviorSubject<HashSet<T>>([.. initial]);
        }

        public IObservable<HashSet<T>> Changes => _subject.DistinctUntilChanged();

        public bool Add(T item)
        {
            var current = _subject.Value;

            if (current.Contains(item))
                return false;

            var updated = new HashSet<T>(current) { item };
            _subject.OnNext(updated);
            return true;
        }

        public bool Remove(T item)
        {
            var current = _subject.Value;

            if (!current.Contains(item))
                return false;

            var updated = new HashSet<T>(current);
            updated.Remove(item);
            _subject.OnNext(updated);
            return true;
        }

        public void Clear()
        {
            if (_subject.Value.Count == 0)
                return;

            _subject.OnNext([]);
        }

        public IEnumerator<T> GetEnumerator() => _subject.Value.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

