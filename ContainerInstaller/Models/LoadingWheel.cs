using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ContainerInstaller.Models
{
    public class LoadingWheel : INotifyPropertyChanged
    {
        private Visibility visibility;
        private static LoadingWheel _instance;
        public static LoadingWheel Instance => _instance ?? (_instance = new LoadingWheel());

        private LoadingWheel() 
        {
            Visibility = Visibility.Hidden;
        }
       
        public static LoadingWheel GetInstance()
        {
            if (_instance == null)
            {
                _instance = new LoadingWheel();
            }
            return _instance;
        }

        public void Active()
        {
            Visibility = Visibility.Visible;
        }

        public void Disabled()
        {
            Visibility = Visibility.Hidden;
        }

        public Visibility Visibility
        {
            get
            {
                return visibility;
            }
            set
            {
                visibility = value;
                Console.WriteLine("Changed " + value);
                OnPropertyChanged("Visibility");
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
