using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ContainerInstaller
{
    interface IDockerComposeFileTemplate
    {
        // Where is the docker-compose template file located
        string RepositoryUrl
        {
            get;
            set;
        }

        // Used to collect the templates from repositories
        WebClient WClient
        {
            get;
        }

        // Options that we should set inside of the docker-compose file when building the container
        // HOST_NAME
        // PORTS 
        // etc.
        Dictionary<string, string> Options
        {
            get;
            set;
        }

        // Downloading docker compose file from repository
        void DownloadDockerComposeTemplate(string outputFilePath);

        void RemapDockerComposeTemplate(string outputFilePath);
    }
}
