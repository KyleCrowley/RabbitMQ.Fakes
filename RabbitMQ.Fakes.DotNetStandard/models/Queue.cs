using RabbitMQ.Client;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RabbitMQ.Fakes.DotNetStandard.Models
{
    public class Queue
    {
        private readonly ConcurrentDictionary<string, IBasicConsumer> _consumers;
        private readonly ConcurrentQueue<string> _consumersQueue;

        public string Name { get; set; }

        public bool IsDurable { get; set; }

        public bool IsExclusive { get; set; }

        public bool IsAutoDelete { get; set; }

        public IDictionary<string, object> Arguments = new Dictionary<string, object>();

        public ConcurrentQueue<RabbitMessage> Messages = new ConcurrentQueue<RabbitMessage>();
        public ConcurrentDictionary<string, ExchangeQueueBinding> Bindings = new ConcurrentDictionary<string, ExchangeQueueBinding>();

        public Queue()
        {
            _consumers = new ConcurrentDictionary<string, IBasicConsumer>();
            _consumersQueue = new ConcurrentQueue<string>();
        }

        public void PublishMessage(RabbitMessage message)
        {
            message.Queue = this.Name;

            this.Messages.Enqueue(message);

            DeliverMessage(message);
        }

        public void AddConsumer(string tag, IBasicConsumer consumer)
        {
            _consumers[tag] = consumer;
            RebuildConsumerQueue();
        }

        public void RemoveConsumer(string tag)
        {
            if (_consumers.TryRemove(tag, out var _))
            {
                RebuildConsumerQueue();
            }
        }

        private void RebuildConsumerQueue()
        {
            while (_consumersQueue.TryDequeue(out var _))
            {
                // Intentionally do nothing.
            }

            foreach (var consumer in _consumers.Keys)
            {
                _consumersQueue.Enqueue(consumer);
            }
        }

        private void DeliverMessage(RabbitMessage message)
        {
            // Simulates round-robin message delivery by dequeuing a consumer, delivering a message to it, and re-enqueuing it.
            if (_consumersQueue.TryDequeue(out var consumerName))
            {
                if (_consumers.TryGetValue(consumerName, out var consumer))
                {
                    consumer.HandleBasicDeliver(consumerName, message.DeliveryTag, false, message.Exchange, message.RoutingKey, message.BasicProperties, message.Body);
                }

                _consumersQueue.Enqueue(consumerName);
            }
        }
    }
}