﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using System.Windows.Media.Animation;
using MahApps.Metro.Controls.Dialogs;

namespace BusinessAccounting
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            // set opacity and visibility to hidden to display objects in design mode
            GridMenu.Opacity = 0;
            GridMenu.Visibility = Visibility.Hidden;
        }

        private void ButtonMenu_Click(object sender, RoutedEventArgs e)
        {
            OpenCloseMenu();   
        }

        private void MenuButtonCash_Click(object sender, RoutedEventArgs e)
        {
            OpenCloseMenu();
            LoadPage(new UserControls.CashPage());
        }

        private void MenuButtonGraphics_Click(object sender, RoutedEventArgs e)
        {
            OpenCloseMenu();
            LoadPage(new UserControls.GraphicsPage());
        }

        private void MenuButtonEmployee_Click(object sender, RoutedEventArgs e)
        {
            OpenCloseMenu();
            LoadPage(new UserControls.EmployeePage());
        }

        private void OpenCloseMenu()
        {
            if (GridMenu.Opacity == 0)
            {
                GridMenu.Visibility = Visibility.Visible;
            }
            DoubleAnimation animation = new DoubleAnimation
            {
                From = GridMenu.Opacity > 0 ? 1 : 0,
                To = GridMenu.Opacity > 0 ? 0 : 1,
                Duration = new Duration(TimeSpan.FromSeconds(0.5))
            };
            animation.Completed += animation_Completed;
            GridMenu.BeginAnimation(OpacityProperty, animation);
        }

        private void animation_Completed(object sender, EventArgs e)
        {
            // hide objects if they are not visible already
            // to avoid clicks
            if (GridMenu.Opacity == 0)
            {
                GridMenu.Visibility = Visibility.Hidden;
            }
        }

        private void LoadPage(UserControl pPage)
        {
            UserControlGrid.Children.Clear();
            UserControlGrid.Children.Add(pPage);
        }

        private bool _windowDisplayed;

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (_windowDisplayed)
                return;

            _windowDisplayed = true;

            // open default page
            LoadPage(new UserControls.CashPage());
        }

        private void OpenDbFolder_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("explorer",
                    $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\{Assembly.GetExecutingAssembly().GetName().Name}");
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
