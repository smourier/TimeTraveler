using System;
using System.Runtime.Serialization;

namespace TimeTraveler
{
    [Serializable]
    public class TimeTravelerException : Exception
    {
        public TimeTravelerException(string message)
            : base(message)
        {
        }

        public TimeTravelerException(Exception innerException)
            : base(null, innerException)
        {
        }

        public TimeTravelerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected TimeTravelerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
