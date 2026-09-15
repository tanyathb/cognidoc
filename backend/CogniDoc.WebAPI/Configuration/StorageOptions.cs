namespace CogniDoc.WebAPI.Configuration
{
    public class StorageOptions
    {
        public const string SectionName = "Storage";
        public string ContainerName { get; set; } = "documents";
    }
}
