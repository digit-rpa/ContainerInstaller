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

namespace ContainerInstaller.ViewModels
{
    class SetupViewModel : ViewModelBase
    {
        private string containerInstallationPath;
        private Helper helper;
        private bool setupSucceeded;

        // Commands
        private ICommand startSetupCommand;

        public SetupViewModel()
        {
            StartSetupCommand = new RelayCommand(StartSetup, param => true);

            // Instantiating helper
            helper = new Helper();

            // WE SHOULD CHECK IF WE HAVE JSON FILE WITH SETUP SETTINGS 
            // IF WE DONT, THEN WE JUST SENT USER TO CONTAINERINSTALLER VIEW
        }

        // This will be called form command later on
        private void StartSetup(object obj)
        {
            // Dynamic object of settings
            dynamic settings = new ExpandoObject();
            settings.ContainerInstallationPath = ContainerInstallationPath;

            SetupSucceeded = true;

            try
            {
                File.WriteAllText(helper.GetExecutionPath() + "setup-settings.json", JsonConvert.SerializeObject(settings));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                SetupSucceeded = false;
            }

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
    }
}
