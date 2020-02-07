using ContainerInstaller.Common;
using ContainerInstaller.Models;
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
        ObservableCollection<string> containerOptions = new ObservableCollection<string>();

        // We need to know if Docker for windows is up and running before we can perform any actions
        bool dockerForWindowsIsRunning;
        
        // We want to inform the user of the current state of operation
        string healthState;

        string dockerComposeFilesBasePath;

        string choosenContainer;

        // Commands
        private ICommand setupContainerCommand;

        // Constructor
        public MainWindowViewModel() {

            // Relay commands 
            SetupContainerCommand = new RelayCommand(SetupContainer, param => true);
            
            // Wich Containers is there to choose from by the user?
            containerOptions.Add("Kibana");

            
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

        private void SetupContainer(object obj)
        {
            if (dockerForWindowsIsRunning)
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
                dockerComposeFile.RepositoryUrl = @"https://raw.githubusercontent.com/digit-rpa/docker-compose-templates/master/kibana-docker-compose.yml";
                dockerComposeFile.Options.Add("ELASTICSEARCH_CONTAINER_NAME", "elasticsearch");
                dockerComposeFile.Options.Add("ELASTICSEARCH_OUTSIDE_PORT", "9200");
                dockerComposeFile.Options.Add("KIBANA_CONTAINER_NAME", "kibana");
                dockerComposeFile.Options.Add("KIBANA_VIRTUAL_HOSTNAME", "kibana.local");
                dockerComposeFile.Options.Add("KIBANA_OUTSIDE_PORT", "5601");
                dockerComposeFile.DownloadDockerComposeTemplate(dockerComposeFilesBasePath + "docker-compose.yml");

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
        public ObservableCollection<string> ContainerOptions { get => containerOptions; set => containerOptions = value; }
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
            }
        }
    }
}
