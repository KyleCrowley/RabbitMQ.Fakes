using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RabbitMQ.Fakes.DotNetStandard.Models
{
    public class DirectExchange : Exchange
    {
        public ConcurrentDictionary<string, IList<Queue>> Bindings { get; }

        public DirectExchange(string name) : base(name, ExchangeType.Direct)
        {
            Bindings = new ConcurrentDictionary<string, IList<Queue>>();
        }

        public override void BindQueue(string bindingKey, Queue queue)
        {
            // No binding has been established between this binding key and queue.
            if (!Bindings.TryGetValue(bindingKey, out var queues))
            {
                queues = new List<Queue>();
            }
            // Handle the case when the queue is being re-bound (duplicate binding).
            else if (queues.Any(q => q.Name == queue.Name))
            {
                throw new InvalidOperationException($"Queue '{queue.Name}' was already bound with the binding key '{bindingKey}'.");
            }

            queues.Add(queue);
            Bindings[bindingKey] = queues;
        }

        public override void UnbindQueue(string bindingKey, Queue queue)
        {
            if (!Bindings.TryGetValue(bindingKey, out var bindings))
            {
                // TODO: Silently fail?
                return;
            }

            bindings.Remove(queue);

            // If there are no more bindings for the specified binding key, remove the KVP.
            if (bindings.Count == 0)
            {
                Bindings.TryRemove(bindingKey, out var _);
            }
        }

        protected override IEnumerable<Queue> GetQueues(RabbitMessage message)
        {
            if (!Bindings.TryGetValue(message.RoutingKey, out var queues))
            {
                return Enumerable.Empty<Queue>();
            }

            return queues;
        }
    }
}