using System;
using System.Collections.Generic;
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

    }
}
