﻿using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RabbitMQ.Fakes.DotNetStandard.Models
{
    public abstract class Exchange
    {
        public string Name { get; }
        public string Type { get; }
        public bool IsDurable { get; set; }
        public bool AutoDelete { get; set; }
        public IDictionary Arguments = new Dictionary<string, object>();

        public ConcurrentDictionary<string, IList<Queue>> QueueBindings = new ConcurrentDictionary<string, IList<Queue>>();

        public Exchange(string name, string type)
        {
            Name = name;
            Type = type;
        }

        public abstract void BindQueue(string bindingKey, Queue queue);

        public abstract void UnbindQueue(string bindingKey, Queue queue);

        public abstract bool PublishMessage(RabbitMessage message);
    }
}