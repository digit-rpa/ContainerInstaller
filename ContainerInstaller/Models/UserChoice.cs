using ContainerInstaller.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContainerInstaller.Models
{
    class UserChoice : INotifyPropertyChanged
    {

        private string userChoiceKey;

        private string userChoicevalue;

        public string UserChoiceKey {
            get
            {
                return userChoiceKey;
            }
            set
            {
                userChoiceKey = value;
                OnPropertyChanged("UserChoiceKey");
            }
        }

        public string UserChoiceValue
        {
            get
            {
                return userChoicevalue;
            }
            set
            {
                userChoicevalue = value;
                OnPropertyChanged("UserChoiceValue");
                FindAndReplaceValue(UserChoiceValue);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public void FindAndReplaceValue(string value)
        {
            foreach (UserChoice userChoice in MainWindowViewModel.userChoices)
            {
                if(userChoice.userChoiceKey == this.userChoiceKey)
                {
                    userChoice.userChoicevalue = value;
                }
            }
        }
    }
}
