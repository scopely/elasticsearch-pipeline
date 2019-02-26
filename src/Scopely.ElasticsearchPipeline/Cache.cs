using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Scopely.Elasticsearch
{
    static class Cache
    {
        private class CachedItem<T>
        {
            public Stopwatch Watch;
            public T Value;
        }

        public static Func<T> Wrap<T>(Func<T> inner) => Wrap(TimeSpan.FromMinutes(5), inner);

        public static Func<T> Wrap<T>(TimeSpan maxAge, Func<T> inner)
        {
            var syncRoot = new object();
            var item = default(CachedItem<T>);
            return () =>
            {
                var i = item;
                if (i == null || i.Watch.Elapsed > maxAge)
                {
                    lock (syncRoot)
                    {
                        if (i == null || i.Watch.Elapsed > maxAge)
                        {
                            i = item = new CachedItem<T>
                            {
                                Value = inner(),
                                Watch = Stopwatch.StartNew(),
                            };
                        }
                    }
                }
                return i.Value;
            };
        }
    }
}
