﻿using ContainerInstaller.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ContainerInstaller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static ContentControl pageContentControl;

        public MainWindow()
        {
            InitializeComponent();

            // Setting the static content control to the pageContentControl of the view
            pageContentControl = PageContentControl;

            // If first time running program on this host, 
            // Ask user if they want to setup environment
            pageContentControl.Content = new SetupView();
        }
    }
}
