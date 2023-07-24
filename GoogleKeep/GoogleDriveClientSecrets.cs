using Google.Apis.Auth.OAuth2;

namespace GoogleKeep
{
    internal class GoogleDriveClientSecretsBase
    {
        public virtual ClientSecrets ClientSecrets { get; } = new ClientSecrets();
    }

    internal partial class GoogleDriveClientSecrets : GoogleDriveClientSecretsBase
    {
    }
}