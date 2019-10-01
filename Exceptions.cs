using System;
using System.Runtime.Serialization;

namespace OblikControl
{
    [Serializable]
    public class OblikIOException : Exception, ISerializable
    {
        public OblikIOException(string message) : base(message) { }
        public OblikIOException() : base() { }
        public OblikIOException(string message, Exception innerException) : base(message, innerException) { }
        protected OblikIOException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
    [Serializable]
    public class OblikSegException : Exception, ISerializable
    {
        public OblikSegException(string message) : base(message) { }
        public OblikSegException() : base() { }
        public OblikSegException(string message, Exception innerException) : base(message, innerException) { }
        protected OblikSegException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class OblikCmdException : Exception, ISerializable
    {
        public OblikCmdException(string message) : base(message) { }
        public OblikCmdException() : base() { }
        public OblikCmdException(string message, Exception innerException) : base(message, innerException) { }
        protected OblikCmdException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
