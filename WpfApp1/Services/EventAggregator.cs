using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp1.Services
{
    public class EventAggregator
    {
        private static readonly EventAggregator _instance = new EventAggregator();
        public static EventAggregator Instance => _instance;

        private readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();

        private EventAggregator() { }

        public void Publish<T>(T message)
        {
            if (_subscribers.ContainsKey(typeof(T)))
            {
                foreach (var subscriber in _subscribers[typeof(T)].ToList()) // ToList để tránh concurrent modification
                {
                    try
                    {
                        ((Action<T>)subscriber)(message);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in event handler: {ex.Message}");
                    }
                }
            }
        }

        public void Subscribe<T>(Action<T> action)
        {
            var messageType = typeof(T);
            if (!_subscribers.ContainsKey(messageType))
            {
                _subscribers[messageType] = new List<Delegate>();
            }
            _subscribers[messageType].Add(action);
        }

        public void Unsubscribe<T>(Action<T> action)
        {
            var messageType = typeof(T);
            if (_subscribers.ContainsKey(messageType))
            {
                _subscribers[messageType].Remove(action);
                
                // Xóa type nếu không còn subscribers
                if (_subscribers[messageType].Count == 0)
                {
                    _subscribers.Remove(messageType);
                }
            }
        }
    }
}