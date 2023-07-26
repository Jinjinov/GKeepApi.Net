using System;
using System.Collections.Generic;

/// <summary>
/// exception.py
/// </summary>
namespace GKeepApi.Net
{
    /// <summary>
    /// The API server returned an error.
    /// </summary>
    public class APIException : Exception
    {
        public int Code { get; }

        public APIException(int code, string message) : base(message)
        {
            Code = code;
        }

        public APIException(int code, string message, Exception innerException) : base(message, innerException)
        {
            Code = code;
        }
    }

    /// <summary>
    /// Generic Keep error.
    /// </summary>
    public class KeepException : Exception
    {
        public KeepException(string message) : base(message) { }
        public KeepException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Login exception.
    /// </summary>
    public class LoginException : KeepException
    {
        public LoginException(string message) : base(message) { }
        public LoginException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Browser login required error.
    /// </summary>
    public class BrowserLoginRequiredException : LoginException
    {
        public string Url { get; }

        public BrowserLoginRequiredException(string url, string message) : base(message)
        {
            Url = url;
        }

        public BrowserLoginRequiredException(string url, string message, Exception innerException) : base(message, innerException)
        {
            Url = url;
        }
    }

    /// <summary>
    /// Keep label error.
    /// </summary>
    public class LabelException : KeepException
    {
        public LabelException(string message) : base(message) { }
        public LabelException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Keep consistency error.
    /// </summary>
    public class SyncException : KeepException
    {
        public SyncException(string message) : base(message) { }
        public SyncException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Full resync required error.
    /// </summary>
    public class ResyncRequiredException : SyncException
    {
        public ResyncRequiredException(string message) : base(message) { }
        public ResyncRequiredException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Upgrade recommended error.
    /// </summary>
    public class UpgradeRecommendedException : SyncException
    {
        public UpgradeRecommendedException(string message) : base(message) { }
        public UpgradeRecommendedException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Node consistency error.
    /// </summary>
    public class MergeException : KeepException
    {
        public MergeException(string message) : base(message) { }
        public MergeException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Constraint error.
    /// </summary>
    public class InvalidException : KeepException
    {
        public InvalidException(string message) : base(message) { }
        public InvalidException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Parse error.
    /// </summary>
    public class ParseException : KeepException
    {
        public Dictionary<string, object> Raw { get; }

        public ParseException(string message, Dictionary<string, object> raw) : base(message)
        {
            Raw = raw;
        }

        public ParseException(string message, Dictionary<string, object> raw, Exception innerException) : base(message, innerException)
        {
            Raw = raw;
        }
    }
}
