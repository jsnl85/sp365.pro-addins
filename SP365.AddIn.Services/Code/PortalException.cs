using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;

namespace SP365.AddIn.Services
{
    [Serializable]
    public class PortalException : Exception, ISerializable
    {
        public PortalException() : base("Unexpected Portal Exception.") { }
        public PortalException(string message) : base(message) { }
        public PortalException(string message, Exception innerException) : base(message, innerException) { }
        public PortalException(string message, HttpStatusCode statusCode, Exception innerException) : this(message, innerException) { this.HttpStatusCode = statusCode; }
        protected PortalException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        // 
        public HttpStatusCode? HttpStatusCode { get; set; }
        // 
        public override void GetObjectData(SerializationInfo info, StreamingContext context) { this.GetObjectData(info, context); }
    }
    [Serializable]
    public class UserNotAuthenticatedException : PortalException, ISerializable
    {
        public UserNotAuthenticatedException() : this("User is not signed-in.", System.Net.HttpStatusCode.Unauthorized, (Exception)null) { }
        public UserNotAuthenticatedException(string message) : this(message, System.Net.HttpStatusCode.Unauthorized, (Exception)null) { }
        public UserNotAuthenticatedException(string message, Exception innerException) : this(message, System.Net.HttpStatusCode.Unauthorized, innerException) { }
        public UserNotAuthenticatedException(string message, HttpStatusCode statusCode, Exception innerException) : base(message, statusCode, innerException) { }
        protected UserNotAuthenticatedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class IdentityResultException : Exception, ISerializable
    {
        public IdentityResultException() : base() { }
        public IdentityResultException(IEnumerable<string> errors) : base(string.Join(Environment.NewLine, errors ?? new List<string>(0))) { this.Errors = errors?.ToList(); }
        public IdentityResultException(string message) : base(message) { }
        public IdentityResultException(string message, Exception innerException) : base(message, innerException) { }
        protected IdentityResultException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        // 
        public List<string> Errors { get; set; }
        public HttpStatusCode? HttpStatusCode { get; set; }
        // 
        public override void GetObjectData(SerializationInfo info, StreamingContext context) { this.GetObjectData(info, context); }
    }
}
