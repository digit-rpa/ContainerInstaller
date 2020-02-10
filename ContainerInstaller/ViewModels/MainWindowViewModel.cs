using ContainerInstaller.Common;
using ContainerInstaller.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ContainerInstaller.ViewModels
{

    class MainWindowViewModel : ViewModelBase
    {
        // List of containers that this program is able to automatically setup
        Dictionary<string, string> containers = new Dictionary<string, string>();

        Dictionary<string, string> containerOptionsUserInput = new Dictionary<string, string>();
        ObservableCollection<UserChoice> userChoices = new ObservableCollection<UserChoice>();

        // We need to know if Docker for windows is up and running before we can perform any actions
        bool dockerForWindowsIsRunning;
        
        // We want to inform the user of the current state of operation
        string healthState;

        string dockerComposeFilesBasePath;

        string choosenContainer;

        SettingsReader settingsReader;

        Helper helper;

        // Commands
        private ICommand setupContainerCommand;

        // Constructor
        public MainWindowViewModel() {

            helper = new Helper();

            // Reading container settings (where is repository foreach container located, and the name of the container choice)
            settingsReader = new SettingsReader();
            dynamic containerSettings = settingsReader.ReadSettingsFromJsonFile(helper.GetExecutionPath() + "settings/container-settings.json");

            // Wich Containers is there to choose from by the user?
            foreach(dynamic container in containerSettings["container-choices"]) {
                string containerName = container["repository-container-folder-name"];
                string containerRepositoryUrl = container["repository-container-url"];
                containers.Add(containerName, containerRepositoryUrl);
            }

            // Relay commands 
            SetupContainerCommand = new RelayCommand(SetupContainer, param => true);
                        
            
            HealthState = "Checking state...";

            // Is Docker running?
            if(CheckProgramIsRunning("Docker Desktop"))
            {
                HealthState = "Ready to work";
                dockerForWindowsIsRunning = true;
            } 
            else
            {
                HealthState = "Error please start Docker for Windows..";
            }
        }

        private void PrepareContainerView(dynamic containerSettings)
        {

        }

        private void SetupContainer(object obj)
        {
            try
            {
                // Setting the choosen container value
                choosenContainer = obj.ToString();
            } catch(Exception e)
            {
                Console.WriteLine(e);
            }

            if (dockerForWindowsIsRunning && !String.IsNullOrEmpty(choosenContainer))
            {

                // This should be the users who sets where to store the actual "new" docker-compose file"
                dockerComposeFilesBasePath = "C://docker-production/" + choosenContainer + "/";

                // Create directory if not exists
                if (!Directory.Exists(dockerComposeFilesBasePath))
                {
                    Directory.CreateDirectory(dockerComposeFilesBasePath);
                }

                // TESTING THESE VALUES SHOULD BE COMMING FROM THE USER
                DockerComposeFile dockerComposeFile = new DockerComposeFile();
                dockerComposeFile.executionPath = helper.GetExecutionPath();

                // WE want to read these settings automaticly from the docker-compose file and expose to the user
                dockerComposeFile.Options.Add("ELASTICSEARCH_CONTAINER_NAME", "elasticsearch");
                dockerComposeFile.Options.Add("ELASTICSEARCH_OUTSIDE_PORT", "9200");
                dockerComposeFile.Options.Add("KIBANA_CONTAINER_NAME", "kibana");
                dockerComposeFile.Options.Add("KIBANA_VIRTUAL_HOSTNAME", "kibana.local");
                dockerComposeFile.Options.Add("KIBANA_OUTSIDE_PORT", "5601");
                dockerComposeFile.DownloadDockerComposeTemplate(containers[choosenContainer] + "docker-compose.yml");
                dockerComposeFile.RemapDockerComposeTemplate(dockerComposeFilesBasePath + "docker-compose.yml");
                dockerComposeFile.CleanTmpFiles();


                // The path to where this docker-compose.yml file should be executed (maybe copied?) 
                // Should come from user input
                string command = "/K cd " + dockerComposeFilesBasePath + " && ";
                command += "docker-compose up -d && ";
                command += "timeout 15 && ";
                command += "exit";

                // Lets try to setup container by docker-compose.yml file inside CMD
                Process cmd = new Process();
                cmd.StartInfo.FileName = "cmd.exe";
                //cmd.StartInfo.UseShellExecute = false;
                //cmd.StartInfo.RedirectStandardOutput = true;
                //cmd.StartInfo.RedirectStandardError = true;
                //cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.Verb = "runas";
                cmd.StartInfo.Arguments = command;
                cmd.Start();

            }
        }

        private bool CheckProgramIsRunning(string programName)
        {
            // Looping throug all processes running at the moment
            foreach(Process process in Process.GetProcesses()) {

                if(process.ProcessName.Contains(programName))
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

        public void UpdateUserChoices()
        {
            DockerComposeFile tmpFile = new DockerComposeFile();
            tmpFile.DownloadContainerInfo(containers[choosenContainer] + "container-info.json");
            dynamic containerOptions = settingsReader.ReadSettingsFromJsonFile(helper.GetExecutionPath() + "container-info.json");
            
            foreach(dynamic options in containerOptions["options"])
            {

                foreach(JProperty option in options.Properties())
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

        public ObservableCollection<UserChoice> UserChoices
        {
            get
            {
                return userChoices;
            }
            set
            {
                userChoices = value;
                OnPropertyChanged("UserChoices");
                Console.WriteLine("test");
            }
        }
    }
}
