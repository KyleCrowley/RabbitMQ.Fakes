using RabbitMQ.Fakes.DotNetStandard.Models;
using System.Collections.Concurrent;

namespace RabbitMQ.Fakes.DotNetStandard
{
    public class RabbitServer
    {
        public ConcurrentDictionary<string, Exchange> Exchanges { get; }

        public ConcurrentDictionary<string, Queue> Queues { get; }

        public DirectExchange DefaultExchange => (DirectExchange)Exchanges[string.Empty];

        public RabbitServer()
        {
            Exchanges = new ConcurrentDictionary<string, Exchange>();
            Queues = new ConcurrentDictionary<string, Queue>();

            InitializeDefaultExchange();
        }

        public void Reset()
        {
            Exchanges.Clear();
            Queues.Clear();

            // Need to re-initialize the default exchange.
            InitializeDefaultExchange();
        }

        private void InitializeDefaultExchange()
        {
            // https://www.rabbitmq.com/tutorials/amqp-concepts.html#exchange-default
            Exchanges[string.Empty] = new DirectExchange(string.Empty);
        }
    }
}