using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ContainerInstaller.Common
{
    class Helper
    {
        public string GetExecutionPath()
        {
            string[] pathParts = Assembly.GetExecutingAssembly().Location.Split('\\');

            pathParts[pathParts.Length - 1] = "";

            string path = String.Join(@"\", pathParts);

            return path;

        }

        public dynamic ReadSettingsFromJsonFile(string filePath)
        {
            // Tmp list of settings
            List<string> settings = new List<string>();

            // Reading the json file into string
            var json = File.ReadAllText(filePath);

            // Deserializing the json string into dynamic object
            dynamic settingsObject = JsonConvert.DeserializeObject(json);

            return settingsObject;

            //Console.WriteLine(settingsObject["container-choices"][0]["repository-container-folder-name"]);
        }

    }
}
