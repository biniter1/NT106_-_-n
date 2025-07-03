using System;
using System.Collections.Generic;

namespace WpfApp1.Services
{
    public class EventAggregator
    {
        private static readonly EventAggregator _instance = new EventAggregator();
        public static EventAggregator Instance => _instance;

        private readonly Dictionary<Type, List<Action<object>>> _subscribers = new Dictionary<Type, List<Action<object>>>();

        private EventAggregator() { }

        public void Publish<T>(T message)
        {
            if (_subscribers.ContainsKey(typeof(T)))
            {
                foreach (var subscriber in _subscribers[typeof(T)])
                {
                    subscriber(message);
                }
            }
        }

        public void Subscribe<T>(Action<T> action)
        {
            var messageType = typeof(T);
            if (!_subscribers.ContainsKey(messageType))
            {
                _subscribers[messageType] = new List<Action<object>>();
            }
            _subscribers[messageType].Add(message => action((T)message));
        }
    }
}