using System;
using System.Runtime.Serialization;

namespace OblikControl
{
    [Serializable]
    public class OblikException : Exception, ISerializable
    {
        public OblikException(string message) : base(message) { }
        public OblikException() : base() { }
        public OblikException(string message, Exception innerException) : base(message, innerException) { }
        protected OblikException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
