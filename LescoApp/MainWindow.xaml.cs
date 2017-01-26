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
using System.Net;
using mshtml;
using System.Threading;
using System.IO;
using LescoApp.Classes;
using System.Windows.Media.Animation;

namespace LescoApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            txtBatchNo.PreviewKeyDown += new KeyEventHandler(numFieldValidation);
            txtSubDiv.PreviewKeyDown += new KeyEventHandler(numFieldValidation);
            txtRefNo.PreviewKeyDown += new KeyEventHandler(numFieldValidation);

            txtLoading.Visibility = Visibility.Hidden;
            circleLoading.Visibility = Visibility.Hidden;
            CompanyView.Visibility = Visibility.Hidden;
            AboutView.Visibility = Visibility.Hidden;
        }
        //Window on load event (Create directories and remove cache files)
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            wbBrowser.Navigate("about:blank");
            txtBatchNo.Focus();

            try
            {
                UtilityClass.createDirectoryStructure();
                UtilityClass.cleanTemp();
            }
            catch
            {
                MessageBox.Show("Some directories/files are missing or inaccessible.\nPlease make sure you have administrator privileges.", "Error Message", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }

            CompanyView.Visibility = Visibility.Visible;
            UtilityClass.customAnimation(this, "fadeIn", CompanyView);
        }
        
        //Search button on click event
        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            //Disable search button to prevent async commands stacking
            wbBrowser.Navigate("about:blank");
            btnSearch.IsEnabled = false;

            //BatchNo, SubDivNo and RefNo Validation
            if (string.IsNullOrEmpty(txtBatchNo.Text) || string.IsNullOrEmpty(txtSubDiv.Text) || string.IsNullOrEmpty(txtRefNo.Text))
            {
                MessageBox.Show("Reference no is empty. Please enter a valid reference no.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                txtBatchNo.Focus();
                btnSearch.IsEnabled = true;
                return;
            }

            //Fade in loading animation
            txtLoading.Visibility = Visibility.Visible;
            circleLoading.Visibility = Visibility.Visible;
            UtilityClass.customAnimation(this, "fadeInLoading", circleLoading);
            UtilityClass.customAnimation(this, "fadeInText", txtLoading);
            await Task.Delay(600);

            //Set web browser to a blank page
            wbBrowser.Navigate("about:blank");
            await Task.Delay(500);

            //Clean any old downloaded files
            try
            {
                UtilityClass.cleanTemp();
            }
            catch
            {
                MessageBox.Show("Unable to clear cached files. Please restart the application.", "Error Message", MessageBoxButton.OK, MessageBoxImage.Error);

                //Fade out loading animation
                UtilityClass.customAnimation(this, "fadeOutLoading", circleLoading);
                UtilityClass.customAnimation(this, "fadeOutText", txtLoading);
                await Task.Delay(600);
                txtLoading.Visibility = Visibility.Hidden;
                circleLoading.Visibility = Visibility.Hidden;

                btnSearch.IsEnabled = true;
                return;
            }

            //Get Lesco bill as PDF document
            try
            {
                LescoBills newBill = new LescoBills(txtBatchNo.Text, txtSubDiv.Text, txtRefNo.Text, cmbRU.Text);
                newBill.currentPath = UtilityClass.getTempPath();
                await Task.Run(() => {
                    newBill.getLescoBillPDF();
                });
            }
            catch
            {
                //MessageBox.Show("Unable to communicate with the remote server. Please check your internet connection and try again.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                wbBrowser.Navigate(string.Format(@"{0}\Data\errorReport.html", Directory.GetCurrentDirectory()));

                //Fade out loading animation
                UtilityClass.customAnimation(this, "fadeOutLoading", circleLoading);
                UtilityClass.customAnimation(this, "fadeOutText", txtLoading);
                await Task.Delay(600);
                txtLoading.Visibility = Visibility.Hidden;
                circleLoading.Visibility = Visibility.Hidden;

                btnSearch.IsEnabled = true;
                return;
            }

            //Redirect web browser to recently downloaded PDF document
            navigatePDF();
        }
        //Clear button on click event
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            wbBrowser.Navigate("about:blank");
            txtBatchNo.Text = "";
            txtSubDiv.Text = "";
            txtRefNo.Text = "";
            cmbRU.Text = "U";
            txtBatchNo.Focus();
        }
        //Restart button on click event
        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            wbBrowser.Navigate("about:blank");
            await Task.Delay(100);

            RefreshWindow refresh = new RefreshWindow();
            refresh.Show();
            this.Close();
        }
        //About button on click event (Developer Info)
        private async void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            btnAbout.IsEnabled = false;
            if (CompanyView.Visibility == Visibility.Visible)
            {
                UtilityClass.customAnimation(this, "fadeOut", CompanyView);
                await Task.Delay(600);

                CompanyView.Visibility = Visibility.Hidden;
                AboutView.Visibility = Visibility.Visible;

                UtilityClass.customAnimation(this, "fadeIn", AboutView);
                await Task.Delay(600);
            }
            else if (AboutView.Visibility == Visibility.Visible)
            {
                UtilityClass.customAnimation(this, "fadeOut", AboutView);
                await Task.Delay(600);

                AboutView.Visibility = Visibility.Hidden;
                CompanyView.Visibility = Visibility.Visible;

                UtilityClass.customAnimation(this, "fadeIn", CompanyView);
                await Task.Delay(600);
            }
            btnAbout.IsEnabled = true;
        }
        
        //Private Methods for navigation and validation
        private async void navigatePDF()
        {
            string mainFile = string.Format(@"{0}\LescoBill.pdf", UtilityClass.getTempPath());
            if (File.Exists(mainFile))
            {
                using (StreamReader myReader = new StreamReader(mainFile))
                {
                    string str = myReader.ReadToEnd();
                    if (str.StartsWith("%PDF"))
                    {
                        wbBrowser.Navigate(mainFile);
                    }
                    else
                    {
                        wbBrowser.Navigate(string.Format(@"{0}\Data\errorReport.html", Directory.GetCurrentDirectory()));
                    }
                }
            }
            //Fade out loading animation
            UtilityClass.customAnimation(this, "fadeOutLoading", circleLoading);
            UtilityClass.customAnimation(this, "fadeOutText", txtLoading);
            await Task.Delay(600);
            txtLoading.Visibility = Visibility.Hidden;
            circleLoading.Visibility = Visibility.Hidden;
            btnSearch.IsEnabled = true;
        }
        private void numFieldValidation(object sender, KeyEventArgs e)
        {
            if ((e.Key < Key.D0 || e.Key > Key.D9))
            {
                if ((e.Key < Key.NumPad0 || e.Key > Key.NumPad9) && (e.Key != Key.Back) && (e.Key != Key.Tab) && (e.Key != Key.Left) && (e.Key != Key.Right) && (e.Key != Key.Enter))
                {
                    e.Handled = true;
                }
            }
        }

        //Auto field forward logic implementation
        private void txtBatchNo_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtBatchNo.Text.Length == 2)
            {
                txtSubDiv.Focus();
            }
        }
        private void txtSubDiv_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtSubDiv.Text.Length == 5)
            {
                txtRefNo.Focus();
            }
        }
        //Auto field backspace logic implementation
        private void txtRefNo_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back && txtRefNo.Text.Length == 0)
            {
                txtSubDiv.Focus();
                e.Handled = true;
            }
        }
        private void txtSubDiv_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back && txtSubDiv.Text.Length == 0)
            {
                txtBatchNo.Focus();
                e.Handled = true;
            }
        }
    }
}
