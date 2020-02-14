using ContainerInstaller.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ContainerInstaller.Models;
using System.Dynamic;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows.Forms;

namespace ContainerInstaller.ViewModels
{
    class SetupViewModel : ViewModelBase
    {
        private string containerInstallationPath;
        private Helper helper;
        private bool setupSucceeded = false;

        // Commands
        private ICommand startSetupCommand;
        private ICommand chooseContainerInstallationPathCommand;

        public SetupViewModel()
        {
            // Instantiating helper
            helper = new Helper();

            // Lets find out if we already are setup
            // If we are, we just sent the user to container intall view
            if (File.Exists(helper.GetExecutionPath() + "setup-settings.json"))
            {
                SetupSucceeded = true;
            }

            StartSetupCommand = new RelayCommand(StartSetup, param => true);
            ChooseContainerInstallationPathCommand = new RelayCommand(ChooseContainerInstallationPath, param => true);
        }

        private void ChooseContainerInstallationPath(object obj)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();

            if(dialog.ShowDialog() == DialogResult.OK)
            {
                ContainerInstallationPath = dialog.SelectedPath;
            }

        }

        // This will be called form command later on
        private void StartSetup(object obj)
        {
            // Dynamic object of settings
            dynamic settings = new ExpandoObject();
            settings.ContainerInstallationPath = ContainerInstallationPath;

            try
            {
                File.WriteAllText(helper.GetExecutionPath() + "setup-settings.json", JsonConvert.SerializeObject(settings));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                SetupSucceeded = false;
            }

            SetupSucceeded = true;

            Console.WriteLine("Setting up stuff");
        }

        public string ContainerInstallationPath
        {
            get
            {
                return containerInstallationPath;
            }
            set
            {
                containerInstallationPath = value;
                OnPropertyChanged("ContainerInstallationPath");
            }
        }

        public ICommand StartSetupCommand { get => startSetupCommand; set => startSetupCommand = value; }
        public bool SetupSucceeded
        {
            get
            {
                return setupSucceeded;
            }
            set
            {
                setupSucceeded = value;
                OnPropertyChanged("SetupSucceeded");
            }
        }

        public ICommand ChooseContainerInstallationPathCommand { get => chooseContainerInstallationPathCommand; set => chooseContainerInstallationPathCommand = value; }
    }
}
