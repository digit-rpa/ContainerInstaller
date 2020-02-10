using System;
using System.Collections.Generic;
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
                Console.WriteLine(UserChoiceKey);
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
                Console.WriteLine(UserChoiceValue);
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
    }
}
