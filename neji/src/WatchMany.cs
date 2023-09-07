using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Kara.Pattern
{
    public class WatchMany
    {
        Dictionary<Type, object> watched = new();
        List<Tuple<List<Type>, Delegate>> watchers = new();
        HashSet<Type> changed_types = new();
        SemaphoreSlim notify_update = new SemaphoreSlim(0);
        public bool TryGet<T>(out T obj) where T : class
        {
            obj = this.Get<T>();
            return (obj != null);
        }
        public T Get<T>() where T : class
        {
            if (!watched.ContainsKey(typeof(T)))
            {
                return null;
            }
            var existed = watched.TryGetValue(typeof(T), out var v);
            return v as T;
        }

        public void Set<T>(T curr) where T : class
        {
            // Debug.Log($"WatchMany.Set::{typeof(T)}");
            var tt = typeof(T);
            watched[tt] = curr;
            changed_types.Add(tt);
            notify_update.Release();
        }
        public void Update<T>(Action<T> callback) where T : class
        {
            var tt = typeof(T);
            bool existed = watched.TryGetValue(tt, out var v);
            if(!existed) {
                Debug.Print($"Update {tt} failed: not existed");
            }
            else
            {
                try {
                    callback(v as T);
                }
                catch(Exception e) {
                    Debug.Print($"Update {tt} failed: {e}");
                }
            }
            changed_types.Add(tt);
            notify_update.Release();
        }
        public Delegate Watch<T>(Action<T> d)
        {
            return WatchMultipleOrSingle(new List<Type> { typeof(T) }, d);
        }
        public Delegate Watch<T1, T2>(Action<T1, T2> d)
        {
            return WatchMultipleOrSingle(new List<Type> { typeof(T1), typeof(T2) }, d);
        }
        public Delegate Watch<T1, T2, T3>(Action<T1, T2, T3> d)
        {
            return WatchMultipleOrSingle(new List<Type> { typeof(T1), typeof(T2), typeof(T3) }, d);
        }
        Delegate WatchMultipleOrSingle(List<Type> types, Delegate d)
        {
            var v = new Tuple<List<Type>, Delegate>(types, d);
            this.watchers.Add(v);
            foreach (var tt in types)
            {
                changed_types.Add(tt);
            }
            notify_update.Release();
            return d;
        }
        public void Unwatch(Delegate d)
        {
            watchers = watchers.Where(w => w.Item2 != d).ToList();
        }
        // 處理監看邏輯的主要函式，非同步執行
        public async Task Run(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Console.WriteLine($"WatchMany.Run: last_check: {last_check}");
                try
                {
                    // await Task.Delay(100, token);
                    // while (notify_update.CurrentCount >= 1)
                    // {
                    //     notify_update.Wait(token);
                    // }
                    await notify_update.WaitAsync(token);
                }
                catch (TaskCanceledException) {
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                try
                {
                    if(changed_types.Count == 0) {
                        continue;
                    }
                    var changed_types_ = changed_types;
                    changed_types = new HashSet<Type>();
                    foreach (var w in this.watchers.ToList())
                    {
                        if (changed_types_.Overlaps(w.Item1))
                        {
                            // Console.WriteLine($"WatchMany.Run: {w.Item1.Count} {w.Item2.Method.Name}");
                            var values = new List<object>();
                            var desc = "";
                            bool has_existed = false;
                            foreach (var tt in w.Item1)
                            {
                                var existed = watched.TryGetValue(tt, out var v);
                                values.Add(v);
                                desc += $"{tt.Name}={v} ";
                                if(existed) has_existed = true;
                            }
                            if(has_existed) {
                                try {
                                    w.Item2.DynamicInvoke(values.ToArray());
                                }
                                catch (TargetInvocationException tie)
                                {
                                    var e = tie.InnerException ?? tie;
                                    Debug.Print($"WatchMany.Run when Invoke: {desc} {e.Message}");
                                    Debug.Print(tie.InnerException.ToString());
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Print($"WatchMany.Run CatchAll: {e.Message}");
                }
            }
        }

    }
}