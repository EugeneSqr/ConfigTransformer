namespace ConfigTransformer
{
    internal class ConfigurationEntry
    {
        public string FilePath { get; set; }

        public string FileName { get; set; }

        public string ParentSubfolder { get; set; }

        public string TransformationFilePath { get; set; }
    }
}
