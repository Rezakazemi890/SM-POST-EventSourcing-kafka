using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CQRS.Core.Messages
{
    public abstract class Message
    {
        public Guid Id { get; set; }
    }
}