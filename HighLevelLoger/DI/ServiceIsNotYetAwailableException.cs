using System;

namespace Common
{
    [Serializable]
    public class ServiceIsNotYetAwailableException : Exception
    {
        public ServiceIsNotYetAwailableException(Type service, object scope) 
            : this($"Could not resolve service: {service} with scope: {scope}")
        {

        }

        public ServiceIsNotYetAwailableException(string message) : base(message)
        {
        }

        public ServiceIsNotYetAwailableException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public ServiceIsNotYetAwailableException()
        {

        }
    }
}
