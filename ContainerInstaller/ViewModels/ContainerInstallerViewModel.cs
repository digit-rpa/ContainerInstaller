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
using System.Threading;
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

        // List of containers that this program is able to automatically setup.
        // The available container choices are listed inside settings/settings.json.
        Dictionary<string, string> containers = new Dictionary<string, string>();

        // Some containers (container choices) might have user input options.
        // We don´t demand the options to be pressent in container-info.json
        // but we will handle them if they are.
        private static ObservableCollection<UserChoice> userChoicesDockerFile = new ObservableCollection<UserChoice>();
        private static ObservableCollection<UserChoice> userChoicesEnvironmentFile = new ObservableCollection<UserChoice>();

        // We want to inform the user of the current state of operation
        // healthState is the "user freindly string" which informs the user if the program has all of its dependencies (Docker for Windows, Composer)
        // And are ready to create/setup containers.
        string healthState;

        // The base installation path where all docker container installed by this program is located.
        string dockerComposeFilesBasePath;

        // The container name of the user choosen container to install/setup.
        string choosenContainer;

        // setupSettings is holding settings that the user selected on first program run.
        private dynamic setupSettings;

        // Helper holds methods that could be useful across the application.
        Helper helper = new Helper();
        
        // Commands
        private ICommand setupContainerCommand;

        public ContainerInstallerViewModel()
        {
            // Relay commands 
            SetupContainerCommand = new RelayCommand(SetupContainerAsync, param => true);

            // Reading container settings 
            // Where is raw template file of docker-compose.yml and the container-info.json located foreach container choice
            dynamic containerSettings = helper.ReadSettingsFromJsonFile(helper.GetExecutionPath() + "settings/container-settings.json");
            
            // Reading setup-settings.json file into dynamic variable used in SetupContainer method.
            setupSettings = helper.ReadSettingsFromJsonFile(helper.GetExecutionPath() + "setup-settings.json");

            // Reading containers that the user could choose from and adding them to the UI - Dictionary<string, string> containers
            foreach (dynamic container in containerSettings["container-choices"])
            {
                string containerName = container["repository-container-folder-name"];
                string containerRepositoryUrl = container["repository-container-url"];

                containers.Add(containerName, containerRepositoryUrl);
            }

            // Default value for the user.
            HealthState = "Checking state...";

            // Is Docker running?
            // Later we will check if Composer is installed.
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

        private async void SetupContainerAsync(object obj)
        {
            Task task = new Task(() => SetupContainer());
            task.Start();
            await task;
        }

        // Executed when user is pressing setup container button in the UI.
        private void SetupContainer()
        {
            // If the user has choosen a container to install
            // And we have dockerForWindowsRunning we start setting up/installing the container
            if (dockerForWindowsIsRunning && !String.IsNullOrEmpty(choosenContainer))
            {

                // Setting the base path of Container installation placement.
                dockerComposeFilesBasePath = setupSettings["ContainerInstallationPath"] + @"\" + choosenContainer + @"\";

                // Instantiating dockerComposeFile Class
                // We use this to manipulate the docker-compose.yml template file.
                DockerComposeFile dockerComposeFile = new DockerComposeFile();
                dockerComposeFile.executionPath = helper.GetExecutionPath();

                // Reading the container-info.yml file.
                // Holds every information needed to install/setup the choosen container.
                dynamic containerInfo = ReadContainerInfoFile();

                // Maybe we have website zip url
                // If it´s pressent we download from it
                // And placing the downloaded file inside the container folder.
                try
                {
                    // Getting the website url from container-info.json.
                    string websiteZipUrl = containerInfo["website-repository-zip"];

                    // Downloading website.
                    dockerComposeFile.DownloadWebSite(websiteZipUrl, helper.GetExecutionPath() + "master.zip");

                    // Unzip project.
                    ZipFile.ExtractToDirectory(helper.GetExecutionPath() + "master.zip", helper.GetExecutionPath() + "workfolder");

                    // Move the unzipped website.
                    foreach (string directory in Directory.EnumerateDirectories(helper.GetExecutionPath() + "workfolder"))
                    {
                        Directory.Move(directory, dockerComposeFilesBasePath);
                    }
                                       
                    // Moving .env.example to .evn.
                    File.Move(dockerComposeFilesBasePath + ".env.example", dockerComposeFilesBasePath + ".env");

                    // Replacing default values variables with userinput values.
                    string[] environmentFileLines = File.ReadAllLines(dockerComposeFilesBasePath + ".env");
                    string[] environmentFileLinesWithReplacements = ReplaceEnvironmentVariables(environmentFileLines);

                    // Emptying the .env file.
                    File.WriteAllText(dockerComposeFilesBasePath + ".env", string.Empty);
                    
                    // Inserting lines wich are replaced with users choices.
                    File.WriteAllLines(dockerComposeFilesBasePath + ".env", environmentFileLinesWithReplacements);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                // Create base installation directory if not exists.
                if (!Directory.Exists(dockerComposeFilesBasePath))
                {
                    Directory.CreateDirectory(dockerComposeFilesBasePath);
                }

                // Setting users choice which the user can choose from in the UI.
                foreach (UserChoice userChoice in userChoicesDockerFile)
                {
                    dockerComposeFile.Options.Add(userChoice.UserChoiceKey, userChoice.UserChoiceValue);
                }

                // If we have path to installation script.
                // Which should be executed after container is installed and running.
                string installationScript = "";
                try
                {
                    installationScript = containerInfo["installation-script-path"];
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                // If the container is using external network to communicate with other containers.
                bool usingExternalNetwork = false;
                try
                {
                    usingExternalNetwork = containerInfo["using-external-network"];
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                // Downloading the docker-compose.yml template file.
                dockerComposeFile.DownloadDockerComposeTemplate(containers[choosenContainer] + "docker-compose.yml");

                // Remapping the docker-compose.yml file with user inputs.
                dockerComposeFile.RemapDockerComposeTemplate(dockerComposeFilesBasePath + "docker-compose.yml");

                // Removing temporary files
                dockerComposeFile.CleanTmpFiles();

                // The path to where this docker-compose.yml file should be executed.
                string command = "/K cd " + dockerComposeFilesBasePath + " && ";

                // If the container is using external network, we setup external network "web".
                if (usingExternalNetwork)
                {
                    command += "docker network create web 2> nul & ";
                }

                // Setting up the container.
                command += "docker-compose up -d && ";

                // If installation script path is pressent.
                // Run docker command to execute the script inside the running container.
                if (!String.IsNullOrEmpty(installationScript))
                {
                    command += "docker exec " + userChoicesDockerFile.Single(x => x.UserChoiceKey == "WEB_CONTAINER_NAME_VALUE").UserChoiceValue.ToString() + " php " + installationScript + " && ";
                }

                // Finish up
                command += "timeout 15 && ";
                command += "exit";

                //Try to clean up from last unsuccessful run.
                try
                {
                    // Deleting temporary files and folders.
                    Directory.Delete(helper.GetExecutionPath() + "workfolder", true);
                    File.Delete(helper.GetExecutionPath() + "master.zip");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                // Lets try to setup container by docker-compose.yml file inside CMD.
                try
                { 
                    Process cmd = new Process();
                    cmd.StartInfo.FileName = "cmd.exe";
                    cmd.StartInfo.Verb = "runas";
                    cmd.StartInfo.Arguments = command;
                    cmd.Start();
                } catch(Exception e)
                {
                    Console.WriteLine(e);
                }

            }
        }

        private string[] ReplaceEnvironmentVariables(string[] environmentFileLines)
        {
            // Temperary list holding lines from environment file.
            // Default lines if not replaced by user defined variables.
            List<string> tmpStringList = new List<string>();

            // Reading the env file and replacing each variables.
            foreach (string line in environmentFileLines)
            {
                bool foundVariable = false;

                // Looping throug all environment variables.
                foreach (UserChoice userChoice in userChoicesEnvironmentFile)
                {
                    // User choice matching variable is found.
                    if (line.Contains(userChoice.UserChoiceKey))
                    {
                        // Replacing the variable
                        foundVariable = true;
                        string newValue = userChoice.UserChoiceKey + "=" + userChoice.UserChoiceValue;
                        tmpStringList.Add(newValue);
                    }
                }

                // If we did not find anything to replace just insert the line as is
                if(!foundVariable)
                {
                    tmpStringList.Add(line);
                }               
            }

            return tmpStringList.ToArray();
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

        private dynamic ReadContainerInfoFile()
        {
            DockerComposeFile tmpFile = new DockerComposeFile();
            tmpFile.DownloadContainerInfo(containers[choosenContainer] + "container-info.json");
            dynamic containerOptions = helper.ReadSettingsFromJsonFile(helper.GetExecutionPath() + "container-info.json");

            return containerOptions;
        }

        private async void UpdateUserChoicesAsync()
        {
            Task<bool> task = new Task<bool>(() => UpdateUserChoices());
            task.Start();
            bool done = await task;
            LoadingWheel.GetInstance().Visibility = Visibility.Hidden;
        }

        // When user is choosing from the list of containers.
        // We update the choosen container options that the user is able to enter.
        public bool UpdateUserChoices()
        {
            dynamic containerOptions = ReadContainerInfoFile();

            // Sending information to the UI Thread.
            Application.Current.Dispatcher.Invoke(delegate
            {
                userChoicesDockerFile.Clear();
                userChoicesEnvironmentFile.Clear();
            });

            // Adding docker compose file variable options
            foreach (dynamic options in containerOptions["options"])
            {
                foreach (JProperty option in options.Properties())
                {

                    // Sending information to the UI Thread.
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        // Adding userchoices to frontend
                        userChoicesDockerFile.Add(
                            new UserChoice
                            {
                                UserChoiceKey = option.Name,
                                UserChoiceValue = option.Value.ToString()
                            }
                       
                        );
                    });
                }
            }

            // Since this might not be present in the file, we will just try
            try { 
                // Adding environment file options
                foreach (dynamic options in containerOptions["environment-variables"])
                {

                    foreach (JProperty option in options.Properties())
                    {
                        // Sending information to the UI Thread.
                        Application.Current.Dispatcher.Invoke(delegate
                        {
                            // Adding userchoices to frontend
                            userChoicesEnvironmentFile.Add(
                                new UserChoice
                                {
                                    UserChoiceKey = option.Name,
                                    UserChoiceValue = option.Value.ToString()
                                }
                            );
                        });
                    }
                }
            } 
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            return true;
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
                LoadingWheel.GetInstance().Visibility = Visibility.Visible;
                UpdateUserChoicesAsync();
            }
        }

        public static ObservableCollection<UserChoice> UserChoicesDockerFile
        {
            get
            {
                return userChoicesDockerFile;
            }
            set
            {
                userChoicesDockerFile = value;
            }
        }

        public static ObservableCollection<UserChoice> UserChoicesEnvironmentFile { get => userChoicesEnvironmentFile; set => userChoicesEnvironmentFile = value; }
    }
}
