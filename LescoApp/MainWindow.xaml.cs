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
using System.Collections;

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

            TextBox[] txtFields = {txtCustomerID, txtBatchNo, txtSubDiv, txtRefNo};

            foreach (TextBox txtField in txtFields)
            {
                txtField.PreviewKeyDown += new KeyEventHandler(numFieldValidation);
                txtField.GotMouseCapture += (object sender, MouseEventArgs e) => txtField.SelectAll();
                txtField.GotFocus += (object sender, RoutedEventArgs e) => txtField.SelectAll();
            }

            txtLoading.Visibility = Visibility.Hidden;
            circleLoading.Visibility = Visibility.Hidden;
            CompanyView.Visibility = Visibility.Hidden;
            AboutView.Visibility = Visibility.Hidden;
        }
        //Window on load event (Create directories and remove cache files)
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            wbBrowser.Navigate("about:blank");
            txtCustomerID.Focus();

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
            bool IsCommercial = string.Equals(cmbType.Text, "Commercial") ? true : false; ;

            //Disable search button to prevent async commands stacking
            wbBrowser.Navigate("about:blank");
            btnSearch.IsEnabled = false;

            //BatchNo, SubDivNo and RefNo Validation
            if (string.IsNullOrEmpty(txtBatchNo.Text) || string.IsNullOrEmpty(txtSubDiv.Text) || string.IsNullOrEmpty(txtRefNo.Text))
            {
                if (string.IsNullOrEmpty(txtCustomerID.Text))
                {
                    MessageBox.Show("Please provide a valid Customer ID or Reference Number.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    txtCustomerID.Focus();
                    btnSearch.IsEnabled = true;
                    return;
                }
            }

            //Fade in loading animation
            txtLoading.Visibility = Visibility.Visible;
            circleLoading.Visibility = Visibility.Visible;
            UtilityClass.customAnimation(this, "fadeInLoading", circleLoading);
            UtilityClass.customAnimation(this, "fadeInText", txtLoading);
            await Task.Delay(600);

            //Set web browser to a blank page
            wbBrowser.Navigate("about:blank");
            await Task.Delay(600);

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
            LescoBills newBill = new LescoBills();
            string customerID = txtCustomerID.Text;
            
            try
            {
                if (txtCustomerID.IsEnabled == true)
                {
                    await Task.Run(() => newBill.customerID(customerID, IsCommercial)); 
                }
                else
                {
                    newBill.referenceNo(txtBatchNo.Text, txtSubDiv.Text, txtRefNo.Text, cmbRU.Text, IsCommercial);
                }

                if (IsCommercial)
                {
                    wbBrowser.Navigate(newBill.getURL);

                    //Fade out loading animation
                    UtilityClass.customAnimation(this, "fadeOutLoading", circleLoading);
                    UtilityClass.customAnimation(this, "fadeOutText", txtLoading);
                    await Task.Delay(600);
                    txtLoading.Visibility = Visibility.Hidden;
                    circleLoading.Visibility = Visibility.Hidden;

                    btnSearch.IsEnabled = true;
                    return;
                }
                else
                {
                    newBill.currentPath = UtilityClass.getTempPath();
                    await Task.Run(() => {
                        newBill.getLescoBillPDF();
                    });
                }
            }
            catch
            {
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
            txtCustomerID.Text = "";
            txtBatchNo.Text = "";
            txtSubDiv.Text = "";
            txtRefNo.Text = "";
            cmbRU.SelectedIndex = 1;
            cmbType.SelectedIndex = 0;
            txtCustomerID.Focus();
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

        /// <summary>
        /// All the text Validations and Checks are programmed below
        /// </summary>
        private void txtBatchNo_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtBatchNo.Text.Length > 0 || txtSubDiv.Text.Length > 0 || txtRefNo.Text.Length > 0)
            {
                txtCustomerID.IsEnabled = false;
            }
            else
            {
                txtCustomerID.IsEnabled = true;
            }

            if (txtBatchNo.Text.Length == 2)
            {
                txtSubDiv.Focus();
            }
        }
        private void txtSubDiv_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtBatchNo.Text.Length > 0 || txtSubDiv.Text.Length > 0 || txtRefNo.Text.Length > 0)
            {
                txtCustomerID.IsEnabled = false;
            }
            else
            {
                txtCustomerID.IsEnabled = true;
            }

            if (txtSubDiv.Text.Length == 5)
            {
                txtRefNo.Focus();
            }
        }
        private void txtRefNo_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtBatchNo.Text.Length > 0 || txtSubDiv.Text.Length > 0 || txtRefNo.Text.Length > 0)
            {
                txtCustomerID.IsEnabled = false;
            }
            else
            {
                txtCustomerID.IsEnabled = true;
            }
        }
        private void txtCustomerID_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtCustomerID.Text.Length > 0)
            {
                txtBatchNo.IsEnabled = false;
                txtSubDiv.IsEnabled = false;
                txtRefNo.IsEnabled = false;
                cmbRU.IsEnabled = false;
            }
            else
            {
                txtBatchNo.IsEnabled = true;
                txtSubDiv.IsEnabled = true;
                txtRefNo.IsEnabled = true;
                cmbRU.IsEnabled = true;
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

        private void Window_Closed(object sender, EventArgs e)
        {
            // Save the property settings
            Properties.Settings.Default.Save();
        }
    }
}
