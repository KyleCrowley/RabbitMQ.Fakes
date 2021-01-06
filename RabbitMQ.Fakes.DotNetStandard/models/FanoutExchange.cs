using RabbitMQ.Client;
using System.Collections.Generic;
using System.Linq;

namespace RabbitMQ.Fakes.DotNetStandard.Models
{
    public class FanoutExchange : Exchange
    {
        private readonly IList<Queue> _queues;

        public FanoutExchange(string name) : base(name, ExchangeType.Fanout)
        {
            _queues = new List<Queue>();
        }

        public override void BindQueue(string bindingKey, Queue queue)
        {
            if (_queues.Any(q => q.Name == queue.Name))
            {
                // TODO: Throw Exception?
                return;
            }

            _queues.Add(queue);
        }

        public override void UnbindQueue(string bindingKey, Queue queue)
        {
            _queues.Remove(queue);
        }

        public override bool PublishMessage(RabbitMessage message)
        {
            if (_queues.Count == 0)
            {
                return false;
            }

            foreach (var queue in _queues)
            {
                queue.PublishMessage(message);
            }

            return true;
        }
    }
}