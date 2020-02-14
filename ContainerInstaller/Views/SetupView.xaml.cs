using ContainerInstaller.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ContainerInstaller.Views
{
    /// <summary>
    /// Interaction logic for SetupPage.xaml
    /// </summary>
    public partial class SetupView : UserControl
    {
        public SetupView()
        {
            InitializeComponent();
        }

        // We need to check state when setup button is pressed (state changed)
        private void InstallationStatus_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Lets make sure setup is done with no errors before navigating
            if (bool.Parse(this.InstallationStatus.Text))
            {
                Console.WriteLine("Moved on");
                MainWindow.pageContentControl.Content = new ContainerInstallerView();
            } else
            {
                Console.WriteLine("Stayed");
            }
        }

        // We need to check the state when we loaded
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Lets make sure setup is done with no errors before navigating
            if (bool.Parse(this.InstallationStatus.Text))
            {
                Console.WriteLine("Moved on");
                MainWindow.pageContentControl.Content = new ContainerInstallerView();
            }
            else
            {
                Console.WriteLine("Stayed");
            }
        }
    }
}
