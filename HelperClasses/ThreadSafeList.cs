using System;
using System.Collections;
using System.Collections.Generic;

// a thread-safe list of elements allowing safe read and write operations in a multithreading environment.
// this ensures data integrity by preventing race conditions through proper synchronization mechanisms.

namespace ParticleAcceleratorMonitoring 

{
    public class ThreadSafeList<T> : IEnumerable<T>
    {
        private readonly object _lockObject = new object();
        private List<T> _list = new List<T>();

        public void Add(T item)
        {
            lock (_lockObject)
            {
                _list.Add(item);
            }
        }

        public T Get(int index)
        {
            lock (_lockObject)
            {
                return _list[index];
            }
        }


        public List<T> GetAll()
        {
            lock (_lockObject)
            {
                return new List<T>(_list);
            }
        }


        public int Count
        {
            get
            {
                lock (_lockObject)
                {
                    return _list.Count;
                }
            }
        }

        public override string ToString()
        {
            string str = "";

            lock (_lockObject)
            {
                foreach (T element in _list)
                {
                    str += element.ToString() + "\n";
                }
            }

            return str;
        }

        public void Sort()
        {
            lock (_lockObject)
            {
                if (typeof(IComparable<T>).IsAssignableFrom(typeof(T)))
                {
                    _list.Sort();
                }
                else
                {
                    throw new InvalidOperationException($"Type {typeof(T)} does not implement IComparable<{typeof(T)}>");
                }
            }
        }


        public void Sort(Comparison<T> comparison)
        {
            lock (_lockObject)
            {
                _list.Sort(comparison);
            }
        }


        public IEnumerator<T> GetEnumerator()
        {
            List<T> snapshot;
            lock (_lockObject)
            {
                // Take a snapshot to avoid threading issues during iteration
                snapshot = new List<T>(_list);
            }

            foreach (var item in snapshot)
            {
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }




}
