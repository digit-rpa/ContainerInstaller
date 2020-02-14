using ContainerInstaller.Common;
using ContainerInstaller.Models;
using ContainerInstaller.Views;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ContainerInstaller.ViewModels
{
    class ContainerInstallerViewModel : ViewModelBase
    {
        // We need to know if Docker for windows is up and running before we can perform any actions
        bool dockerForWindowsIsRunning;

        // List of containers that this program is able to automatically setup
        Dictionary<string, string> containers = new Dictionary<string, string>();

        Dictionary<string, string> containerOptionsUserInput = new Dictionary<string, string>();
        public static ObservableCollection<UserChoice> userChoices = new ObservableCollection<UserChoice>();

        // We want to inform the user of the current state of operation
        string healthState;

        string dockerComposeFilesBasePath;

        string choosenContainer;

        private dynamic setupSettings;

        Helper helper;

        // Commands
        private ICommand setupContainerCommand;

        public ContainerInstallerViewModel()
        {
            helper = new Helper();

            // Reading container settings (where is repository foreach container located, and the name of the container choice)
            dynamic containerSettings = helper.ReadSettingsFromJsonFile(helper.GetExecutionPath() + "settings/container-settings.json");
            // Setup settings is where all of the program settings are placed
            setupSettings = helper.ReadSettingsFromJsonFile(helper.GetExecutionPath() + "setup-settings.json");
            
            // Wich Containers is there to choose from by the user?
            foreach (dynamic container in containerSettings["container-choices"])
            {
                string containerName = container["repository-container-folder-name"];
                string containerRepositoryUrl = container["repository-container-url"];

                containers.Add(containerName, containerRepositoryUrl);
            }

            // Relay commands 
            SetupContainerCommand = new RelayCommand(SetupContainer, param => true);

            HealthState = "Checking state...";

            // Is Docker running?
            if (CheckProgramIsRunning("Docker Desktop"))
            {
                HealthState = "Ready to work";
                dockerForWindowsIsRunning = true;
            }
            else
            {
                HealthState = "Error please start Docker for Windows..";
            }
        }

        private void SetupContainer(object obj)
        {
            try
            {
                // Setting the choosen container value
                choosenContainer = obj.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            if (dockerForWindowsIsRunning && !String.IsNullOrEmpty(choosenContainer))
            {

                // This should be the users who sets where to store the actual "new" docker-compose file"
                dockerComposeFilesBasePath = setupSettings["ContainerInstallationPath"] + @"\" + choosenContainer + @"\";

                // TESTING THESE VALUES SHOULD BE COMMING FROM THE USER
                DockerComposeFile dockerComposeFile = new DockerComposeFile();
                dockerComposeFile.executionPath = helper.GetExecutionPath();

                dynamic containerInfo = ReadContainerInfoFile();

                // Maybe we have website zip url, if we have, then we download it to the container folder
                try
                {
                    string websiteZipUrl = containerInfo["website-repository-zip"];
                    // Only do this if we have website-zip as part of container-info.json
                    dockerComposeFile.DownloadWebSite(websiteZipUrl, helper.GetExecutionPath() + "master.zip");

                    // Unzip project
                    ZipFile.ExtractToDirectory(helper.GetExecutionPath() + "master.zip", helper.GetExecutionPath() + "workfolder");

                    foreach (string directory in Directory.EnumerateDirectories(helper.GetExecutionPath() + "workfolder"))
                    {
                        Directory.Move(directory, dockerComposeFilesBasePath);

                    }
                    // DELETE THE WORKFOLDER AND repos master zip
                    Directory.Delete(helper.GetExecutionPath() + "workfolder");
                    File.Delete(helper.GetExecutionPath() + "master.zip");

                    // We need to move .env.example to .evn
                    File.Move(dockerComposeFilesBasePath + ".env.example", dockerComposeFilesBasePath + ".env");

                    if (!String.IsNullOrEmpty(userChoices.Single(x => x.UserChoiceKey == "VIRTUAL_HOST_VALUE").UserChoiceValue.ToString()))
                    {
                        File.AppendAllText(dockerComposeFilesBasePath + ".env", "VIRTUAL_HOST=" + userChoices.Single(x => x.UserChoiceKey == "VIRTUAL_HOST_VALUE").UserChoiceValue.ToString() + " \r\n");
                        File.AppendAllText(dockerComposeFilesBasePath + ".env", "DB_ROOT_PASSWORD=" + userChoices.Single(x => x.UserChoiceKey == "DATABASE_ROOT_PASSWORD_VALUE").UserChoiceValue.ToString() + " \r\n");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                // Create directory if not exists
                if (!Directory.Exists(dockerComposeFilesBasePath))
                {
                    Directory.CreateDirectory(dockerComposeFilesBasePath);
                }

                // Setting users choice
                foreach (UserChoice userChoice in userChoices)
                {
                    dockerComposeFile.Options.Add(userChoice.UserChoiceKey, userChoice.UserChoiceValue);
                }

                // If we have installation script to execute after container is running
                string installationScript = "";
                try
                {
                    installationScript = containerInfo["installation-script-path"];
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                // If the container is using external network to communicate with other containers
                bool usingExternalNetwork = false;
                try
                {
                    usingExternalNetwork = containerInfo["using-external-network"];
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                dockerComposeFile.DownloadDockerComposeTemplate(containers[choosenContainer] + "docker-compose.yml");
                dockerComposeFile.RemapDockerComposeTemplate(dockerComposeFilesBasePath + "docker-compose.yml");

                dockerComposeFile.CleanTmpFiles();

                // The path to where this docker-compose.yml file should be executed (maybe copied?) 
                // Should come from user input
                string command = "/K cd " + dockerComposeFilesBasePath + " && ";

                // If the container is using external network, we setup external network "web"
                if (usingExternalNetwork)
                {
                    command += "docker network create web 2> nul & ";
                }

                command += "docker-compose up -d && ";

                if (!String.IsNullOrEmpty(installationScript))
                {
                    command += "docker exec " + userChoices.Single(x => x.UserChoiceKey == "CONTAINER_NAME_VALUE").UserChoiceValue.ToString() + " php " + installationScript + " && ";
                }

                command += "timeout 15 && ";
                command += "exit";

                // Lets try to setup container by docker-compose.yml file inside CMD
                Process cmd = new Process();
                cmd.StartInfo.FileName = "cmd.exe";
                cmd.StartInfo.Verb = "runas";
                cmd.StartInfo.Arguments = command;
                cmd.Start();

            }
        }

        private bool CheckProgramIsRunning(string programName)
        {
            // Looping throug all processes running at the moment
            foreach (Process process in Process.GetProcesses())
            {

                if (process.ProcessName.Contains(programName))
                {
                    return true;
                }
            }
            return false;
        }

        // Getters and Setters
        public Dictionary<string, string> Containers
        {
            get
            {
                return containers;
            }
            set
            {
                containers = value;
                OnPropertyChanged("ContainerOptions");
            }
        }

        public string HealthState
        {
            get
            {
                return healthState;
            }
            set
            {
                healthState = value;
                OnPropertyChanged("HealthState");
            }
        }

        public ICommand SetupContainerCommand { get => setupContainerCommand; set => setupContainerCommand = value; }

        public string ChoosenContainer
        {
            get
            {
                return choosenContainer;
            }
            set
            {
                choosenContainer = value;
                OnPropertyChanged("ChoosenContainer");
                UpdateUserChoices();
            }
        }

        private dynamic ReadContainerInfoFile()
        {
            DockerComposeFile tmpFile = new DockerComposeFile();
            tmpFile.DownloadContainerInfo(containers[choosenContainer] + "container-info.json");
            dynamic containerOptions = helper.ReadSettingsFromJsonFile(helper.GetExecutionPath() + "container-info.json");

            return containerOptions;
        }

        public void UpdateUserChoices()
        {
            dynamic containerOptions = ReadContainerInfoFile();

            userChoices.Clear();

            foreach (dynamic options in containerOptions["options"])
            {

                foreach (JProperty option in options.Properties())
                {
                    // Adding userchoices to frontend
                    userChoices.Add(
                        new UserChoice
                        {
                            UserChoiceKey = option.Name,
                            UserChoiceValue = option.Value.ToString()
                        }
                    );
                }
            }
        }

        public static ObservableCollection<UserChoice> UserChoices
        {
            get
            {
                return userChoices;
            }
            set
            {
                userChoices = value;
            }
        }



    }
}
