using System;

/// <summary>
/// exception.py
/// </summary>
namespace GoogleKeep
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
    }

    /// <summary>
    /// Generic Keep error.
    /// </summary>
    public class KeepException : Exception
    {
        public KeepException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Login exception.
    /// </summary>
    public class LoginException : KeepException
    {
        public LoginException(string message) : base(message)
        {
        }
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
    }

    /// <summary>
    /// Keep label error.
    /// </summary>
    public class LabelException : KeepException
    {
        public LabelException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Keep consistency error.
    /// </summary>
    public class SyncException : KeepException
    {
        public SyncException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Full resync required error.
    /// </summary>
    public class ResyncRequiredException : SyncException
    {
        public ResyncRequiredException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Upgrade recommended error.
    /// </summary>
    public class UpgradeRecommendedException : SyncException
    {
        public UpgradeRecommendedException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Node consistency error.
    /// </summary>
    public class MergeException : KeepException
    {
        public MergeException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Constraint error.
    /// </summary>
    public class InvalidException : KeepException
    {
        public InvalidException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Parse error.
    /// </summary>
    public class ParseException : KeepException
    {
        public string Raw { get; }

        public ParseException(string message, string raw) : base(message)
        {
            Raw = raw;
        }
    }
}
