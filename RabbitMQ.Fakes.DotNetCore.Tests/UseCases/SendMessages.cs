using FluentAssertions;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Fakes.DotNetStandard;
using System.Text;

namespace RabbitMQ.Fakes.DotNetCore.Tests.UseCases
{
    [TestFixture]
    public class SendMessages
    {
        // TODO: Necessary test case?
        // If a message is published to an exchange with no bound queues in "real" RabbitMQ, the message will be silently dropped.
        /*
        [Test]
        public void SendToExchangeOnly()
        {
            // Arrange
            var rabbitServer = new RabbitServer();

            using var connection = new FakeConnectionFactory(rabbitServer).CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare("my_exchange", ExchangeType.Direct);

            const string message = "hello world!";
            var messageBody = Encoding.ASCII.GetBytes(message);

            // Act
            channel.BasicPublish(exchange: "my_exchange", routingKey: "foobar", mandatory: false, basicProperties: null, body: messageBody);

            // Assert
            rabbitServer.Exchanges["my_exchange"].Messages.Count.Should().Be(1);
        }
        */

        [Test]
        public void SendToExchangeWithBoundQueue()
        {
            // Arrange
            var rabbitServer = new RabbitServer();

            ConfigureQueueBinding(rabbitServer, "my_exchange", "some_queue", "foobar");

            using var connection = new FakeConnectionFactory(rabbitServer).CreateConnection();
            using var channel = connection.CreateModel();

            const string message = "hello world!";
            var messageBody = Encoding.ASCII.GetBytes(message);

            // Act
            channel.BasicPublish(exchange: "my_exchange", routingKey: "foobar", mandatory: false, basicProperties: null, body: messageBody);

            // Assert
            rabbitServer.Queues["some_queue"].Messages.Count.Should().Be(1);
        }

        [Test]
        public void SendToExchangeWithMultipleBoundQueues()
        {
            // Arrange
            var rabbitServer = new RabbitServer();

            ConfigureQueueBinding(rabbitServer, "my_exchange", "some_queue", "foobar");
            ConfigureQueueBinding(rabbitServer, "my_exchange", "some_other_queue", "foobar");

            using var connection = new FakeConnectionFactory(rabbitServer).CreateConnection();
            using var channel = connection.CreateModel();

            const string message = "hello world!";
            var messageBody = Encoding.ASCII.GetBytes(message);

            // Act
            channel.BasicPublish(exchange: "my_exchange", routingKey: "foobar", mandatory: false, basicProperties: null, body: messageBody);

            // Assert
            rabbitServer.Queues["some_queue"].Messages.Count.Should().Be(1);
            rabbitServer.Queues["some_other_queue"].Messages.Count.Should().Be(1);
        }

        private void ConfigureQueueBinding(RabbitServer rabbitServer, string exchangeName, string queueName, string routingKey)
        {
            var connectionFactory = new FakeConnectionFactory(rabbitServer);
            using (var connection = connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
                channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Direct);

                channel.QueueBind(queueName, exchangeName, routingKey);
            }
        }
    }
}