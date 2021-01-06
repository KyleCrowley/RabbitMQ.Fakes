using RabbitMQ.Client;
using System;

namespace RabbitMQ.Fakes.DotNetStandard.Models
{
    public class ExchangeFactory
    {
        public static ExchangeFactory Instance => new ExchangeFactory();

        public Exchange GetExchange(string name, string type)
        {
            switch (type)
            {
                case ExchangeType.Direct:
                    return new DirectExchange(name);

                case ExchangeType.Fanout:
                    return new FanoutExchange(name);

                default:
                    throw new NotSupportedException($"Exchange type '{type}' is not supported.");
            }
        }
    }
}