using RabbitMQ.Client;

namespace RabbitMQ.Fakes.DotNetStandard.Models
{
    public class RabbitMessage
    {
        public string Exchange { get; set; }
        public string RoutingKey { get; set; }
        public string Queue { get; set; }
        public bool Mandatory { get; set; }
        public bool Immediate { get; set; }
        public IBasicProperties BasicProperties { get; set; }
        public byte[] Body { get; set; }
        public ulong DeliveryTag { get; set; }

        public RabbitMessage Copy()
        {
            return new RabbitMessage
            {
                Exchange = Exchange,
                RoutingKey = RoutingKey,
                Queue = Queue,
                Mandatory = Mandatory,
                Immediate = Immediate,
                BasicProperties = BasicProperties,
                Body = Body,
                DeliveryTag = DeliveryTag
            };
        }
    }
}