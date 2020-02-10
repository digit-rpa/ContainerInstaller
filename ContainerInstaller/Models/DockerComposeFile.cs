using ContainerInstaller.Common;
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
        public Dictionary<string, string> Options { get; set; }
        public WebClient WClient { get; private set; }
        public string Test { get; set; }
        public string executionPath { get; set; }

        private Helper helper;


        public DockerComposeFile()
        {

            helper = new Helper();

            WClient = new WebClient();
            Options = new Dictionary<string, string>();
        }

        public void DownloadDockerComposeTemplate(string RepositoryDockerComposeFileUrl)
        {
            WClient.DownloadFile(RepositoryDockerComposeFileUrl, helper.GetExecutionPath() + tmpFileName);
        }

        public void DownloadContainerInfo(string RepositoryContainerInfoFileUrl)
        {
            WClient.DownloadFile(RepositoryContainerInfoFileUrl, helper.GetExecutionPath() + "container-info.json");
        }

        public void CleanTmpFiles()
        {
            File.Delete(helper.GetExecutionPath() + tmpFileName);

            File.Delete(helper.GetExecutionPath() + "container-info.json");
        }

        public void RemapDockerComposeTemplate(string outputFilePath)
        {
            string fileContent = File.ReadAllText(helper.GetExecutionPath() + tmpFileName);

            // We now replace all options that the users have choosen inside of the docker-compose file 
            // and resaving the file where it should be used by docker
            foreach(KeyValuePair<string, string> kv in Options)
            {

                fileContent = fileContent.Replace(kv.Key, kv.Value);
            }

            File.WriteAllText(outputFilePath, fileContent);
            
        }
    }
}
