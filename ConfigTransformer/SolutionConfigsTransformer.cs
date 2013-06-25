using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using log4net;

using Microsoft.Web.XmlTransform;
using Match = System.Text.RegularExpressions.Match;

namespace ConfigTransformer
{
    public class SolutionConfigsTransformer
    {
        private const string c_buildConfigRegexGroupName = "bconf";
        private readonly ILog m_logger = LogManager.GetLogger(typeof(SolutionConfigsTransformer));

        public SolutionConfigsTransformer(string sourceDirectory, string targetDirectory, string buildConfiguration)
        {
            SourceDirectory = sourceDirectory;
            TargetDirectory = targetDirectory;
            BuildConfiguration = buildConfiguration;
            FilesToExclude = new List<string>();
        }

        public IList<string> FilesToExclude { get; set; }

        public string SourceDirectory { get; private set; }

        public string TargetDirectory { get; private set; }

        public string BuildConfiguration { get; private set; }

        public void Transform()
        {
            if (!IsInputValid())
            {
                return;
            }

            IList<ConfigurationEntry> configurationEntries = GetConfigurationEntries();
            foreach (ConfigurationEntry entry in configurationEntries)
            {
                var transformation = new XmlTransformation(entry.TransformationFilePath);
                var transformableDocument = new XmlTransformableDocument();
                transformableDocument.Load(entry.FilePath);
                if (transformation.Apply(transformableDocument))
                {
                    if (!string.IsNullOrWhiteSpace(entry.FileName))
                    {
                        var targetDirecory = Path.Combine(TargetDirectory, entry.ParentSubfolder);
                        Directory.CreateDirectory(targetDirecory);
                        transformableDocument.Save(Path.Combine(targetDirecory, entry.FileName));
                    }
                }
            }
        }

        private IList<ConfigurationEntry> GetConfigurationEntries()
        {
            string[] configs = Directory.GetFiles(SourceDirectory, "*.config", SearchOption.AllDirectories);
            var result = new List<ConfigurationEntry>();
            if (configs.Length == 0)
            {
                return result;
            }

            int i = 0;
            while (i < configs.Length - 1)
            {
                string config = configs[i];
                string transformation = configs[i + 1];
                var regex = new Regex(BuildSearchPattern(config.Remove(config.Length - 7, 7)), RegexOptions.IgnoreCase);
                bool found = false;
                while (regex.IsMatch(transformation))
                {
                    Match match = regex.Match(transformation);
                    if (IsTransformationFound(match) && !found)
                    {
                        found = true;
                        if (FilesToExclude.Contains(config))
                        {
                            m_logger.InfoFormat("{0} is in a black list. Won't be processed", config);
                        }
                        else
                        {
                            var entry = new ConfigurationEntry
                            {
                                FilePath = config,
                                FileName = Path.GetFileName(config),
                                ParentSubfolder = GetParentSubfolder(config),
                                TransformationFilePath = transformation
                            };
                            result.Add(entry);
                        }
                    }

                    i++;
                    if (i < configs.Length - 1)
                    {
                        transformation = configs[i + 1];
                    }
                    else
                    {
                        break;
                    }
                }

                i++;
            }

            return result;
        }

        private string BuildSearchPattern(string configName)
        {
            string pattern = string.Format(
                @"^{0}\.(?<{1}>.*)\.config", 
                Regex.Escape(configName),
                c_buildConfigRegexGroupName);
            m_logger.DebugFormat("Regex pattern for {0} is {1}", configName, pattern);
            return pattern;
        }

        private string GetParentSubfolder(string config)
        {
            string directory = Path.GetDirectoryName(config);
            if (string.IsNullOrEmpty(directory))
            {
                m_logger.ErrorFormat("Can't get directory name for {0}", config);
                return string.Empty;
            }

            string parentSubfolder = directory.Replace(SourceDirectory, string.Empty).TrimStart('\\', '/');
            m_logger.DebugFormat("Parent subfolder for {0} is {1}", config, parentSubfolder);
            return parentSubfolder;
        }

        private bool IsTransformationFound(Match match)
        {
            bool found = match.Groups[c_buildConfigRegexGroupName].Value.Equals(
                BuildConfiguration, StringComparison.InvariantCultureIgnoreCase);
            m_logger.DebugFormat(
                found
                    ? "Current transformation matches the build configuration {0}"
                    : "Current transformation doesn't match the build configuration {0}", 
                    BuildConfiguration);

            return found;
        }

        private bool IsInputValid()
        {
            bool result = Directory.Exists(SourceDirectory) 
                && Directory.Exists(TargetDirectory) 
                && !string.IsNullOrWhiteSpace(BuildConfiguration);

            if (!result)
            {
                m_logger.Error("Input is invalid.");
            }

            return result;
        }
    }
}
