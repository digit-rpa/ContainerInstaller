using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ContainerInstaller.Models
{
    public class DockerComposeFile : IDockerComposeFileTemplate
    {

        private string tmpFileName = "tmp-docker-compose.yml";
        public string RepositoryUrl { get; set; }
        public Dictionary<string, string> Options { get; set; }
        public WebClient WClient { get; private set; }
        public string Test { get; set; }

        public DockerComposeFile()
        {

            WClient = new WebClient();
            Options = new Dictionary<string, string>();

            // Saving the compose file when remapped (maybe we should do this somewhere else)
            //File.WriteAllText(GetExecutionPath() + "temp-docker-compose-file.yml", "");

            //Console.WriteLine(RepositoryUrl);

        }

        public void DownloadDockerComposeTemplate(string outputFilePath)
        {
            WClient.DownloadFile(RepositoryUrl, GetExecutionPath() + tmpFileName);
            RemapDockerComposeTemplate(outputFilePath);
        }

        public void RemapDockerComposeTemplate(string outputFilePath)
        {
            string fileContent = File.ReadAllText(GetExecutionPath() + tmpFileName);

            // We now replace all options that the users have choosen inside of the docker-compose file 
            // and resaving the file where it should be used by docker
            foreach(KeyValuePair<string, string> kv in Options)
            {

                fileContent = fileContent.Replace(kv.Key, kv.Value);
            }

            File.WriteAllText(outputFilePath, fileContent);
            
        }

        // We might want to move this out inside a helper class
        private string GetExecutionPath()
        {
            string[] pathParts = Assembly.GetExecutingAssembly().Location.Split('\\');

            pathParts[pathParts.Length - 1] = "";

            string path = String.Join(@"\", pathParts);

            return path;

        }
    }
}
