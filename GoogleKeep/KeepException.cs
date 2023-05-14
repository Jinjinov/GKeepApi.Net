using System;

namespace GoogleKeep
{
    public class APIException : Exception
    {
        public int Code { get; }

        public APIException(int code, string msg) : base(msg)
        {
            Code = code;
        }
    }

    public class KeepException : Exception
    {
        public KeepException(string message) : base(message)
        {
        }
    }

    public class LoginException : KeepException
    {
        public LoginException(string message) : base(message)
        {
        }
    }

    public class BrowserLoginRequiredException : LoginException
    {
        public string Url { get; }

        public BrowserLoginRequiredException(string url) : base("Browser login required error.")
        {
            Url = url;
        }
    }

    public class LabelException : KeepException
    {
        public LabelException(string message) : base(message)
        {
        }
    }

    public class SyncException : KeepException
    {
        public SyncException(string message) : base(message)
        {
        }
    }

    public class ResyncRequiredException : SyncException
    {
        public ResyncRequiredException() : base("Full resync required error.")
        {
        }
    }

    public class UpgradeRecommendedException : SyncException
    {
        public UpgradeRecommendedException() : base("Upgrade recommended error.")
        {
        }
    }

    public class MergeException : KeepException
    {
        public MergeException(string message) : base(message)
        {
        }
    }

    public class InvalidException : KeepException
    {
        public InvalidException(string message) : base(message)
        {
        }
    }

    public class ParseException : KeepException
    {
        public string Raw { get; }

        public ParseException(string message, string raw) : base(message)
        {
            Raw = raw;
        }
    }
}
