namespace ExpressControls
{
    public interface ISecretsManager
    {
        string APIKey { get; }
    }

    public class MissingSecretsManager : ISecretsManager
    {
        public string APIKey => "";
    }
}
