using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace JMW.Types
{
    // Shamelessly taken from: https://github.com/thomaslevesque/WeakEvent
    public class WeakEventSource<TEventArgs> where TEventArgs : EventArgs
    {
        private readonly List<WeakDelegate> _handlers;
        public int HandlerCount => _handlers.Count;

        public WeakEventSource()
        {
            _handlers = [];
        }

        public void Clear()
        {
            lock (_handlers)
            {
                _handlers.Clear();
            }
        }

        public void Raise(object sender, TEventArgs e)
        {
            lock (_handlers)
            {
                // compact in place, dropping handlers whose target was collected, without allocating a predicate
                var kept = 0;
                for (var i = 0; i < _handlers.Count; i++)
                {
                    var handler = _handlers[i];
                    if (!handler.Invoke(sender, e))
                        continue;

                    _handlers[kept++] = handler;
                }

                if (kept < _handlers.Count)
                    _handlers.RemoveRange(kept, _handlers.Count - kept);
            }
        }

        public void Subscribe(EventHandler<TEventArgs> handler)
        {
            var weakHandlers = handler
                .GetInvocationList()
                .Select(d => new WeakDelegate(d))
                .ToList();

            lock (_handlers)
            {
                _handlers.AddRange(weakHandlers);
            }
        }

        public void Unsubscribe(EventHandler<TEventArgs> handler)
        {
            var parts = handler.GetInvocationList();

            lock (_handlers)
            {
                // like delegate removal, take each part of a multicast handler off once, most recent first
                for (var p = parts.Length - 1; p >= 0; p--)
                {
                    var part = parts[p];
                    int index = _handlers.FindLastIndex(h => h.IsMatch(part));

                    if (index >= 0)
                        _handlers.RemoveAt(index);
                }
            }
        }

        private class WeakDelegate
        {
            #region Open handler generation and cache

            private delegate void OpenEventHandler(object? target, object sender, TEventArgs e);

            // ReSharper disable once StaticMemberInGenericType (by design)
            private static readonly ConcurrentDictionary<MethodInfo, OpenEventHandler> _openHandlerCache =
                new();

            private static OpenEventHandler CreateOpenHandler(MethodInfo method)
            {
                ArgumentNullException.ThrowIfNull(method);

                var target = Expression.Parameter(typeof(object), "target");
                var sender = Expression.Parameter(typeof(object), "sender");
                var e = Expression.Parameter(typeof(TEventArgs), "e");

                if (method.IsStatic)
                {
                    var expr = Expression.Lambda<OpenEventHandler>(
                        Expression.Call(
                            method,
                            sender, e),
                        target, sender, e);
                    return expr.Compile();
                }
                else
                {
                    if (method.DeclaringType is null)
                        throw new InvalidOperationException();

                    var expr = Expression.Lambda<OpenEventHandler>(
                        Expression.Call(
                            Expression.Convert(target, method.DeclaringType),
                            method,
                            sender, e),
                        target, sender, e);
                    return expr.Compile();
                }
            }

            #endregion Open handler generation and cache

            // a closure object is referenced only by the delegate, so holding it weakly would let it be collected
            // while the subscriber still expects the handler to fire
            private readonly object? _strongTarget;
            private readonly WeakReference? _weakTarget;
            private readonly MethodInfo _method;
            private readonly OpenEventHandler _openHandler;

            public WeakDelegate(Delegate handler)
            {
                var target = handler.Target;
                if (target is not null)
                {
                    if (IsCompilerGenerated(target.GetType()))
                        _strongTarget = target;
                    else
                        _weakTarget = new WeakReference(target);
                }

                _method = handler.Method;
                _openHandler = _openHandlerCache.GetOrAdd(_method, CreateOpenHandler);
            }

            private static bool IsCompilerGenerated(Type type)
            {
                for (Type? t = type; t is not null; t = t.DeclaringType)
                {
                    if (t.IsDefined(typeof(CompilerGeneratedAttribute), false))
                        return true;
                }

                return false;
            }

            private object? CurrentTarget => _strongTarget ?? _weakTarget?.Target;

            public bool Invoke(object sender, TEventArgs e)
            {
                var target = CurrentTarget;
                if (target is null && _weakTarget is not null)
                    return false;

                // a static handler has no target, and its open handler ignores the target argument
                _openHandler(target, sender, e);
                return true;
            }

            public bool IsMatch(Delegate handler)
            {
                if (!handler.Method.Equals(_method))
                    return false;

                if (_method.IsStatic)
                    return true;

                var target = CurrentTarget;
                return target is not null && ReferenceEquals(handler.Target, target);
            }
        }
    }
}