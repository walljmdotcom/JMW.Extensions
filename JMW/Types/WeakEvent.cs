using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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
                _handlers.RemoveAll(h => !h.Invoke(sender, e));
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

            private readonly WeakReference? _weakTarget;
            private readonly MethodInfo _method;
            private readonly OpenEventHandler _openHandler;

            public WeakDelegate(Delegate handler)
            {
                _weakTarget = handler.Target is not null ? new WeakReference(handler.Target) : null;
                _method = handler.Method;
                _openHandler = _openHandlerCache.GetOrAdd(_method, CreateOpenHandler);
            }

            public bool Invoke(object sender, TEventArgs e)
            {
                object? target = null;
                if (_weakTarget is not null)
                {
                    target = _weakTarget.Target;
                    if (target is null)
                        return false;
                }

                // a static handler has no target, and its open handler ignores the target argument
                _openHandler(target, sender, e);
                return true;
            }

            public bool IsMatch(Delegate handler)
            {
                if (_weakTarget is null && !_method.IsStatic) return false;
                return _method.IsStatic
                    ? handler.Method.Equals(_method)
                    : ReferenceEquals(handler.Target, _weakTarget?.Target)
                    && handler.Method.Equals(_method);
            }
        }
    }
}