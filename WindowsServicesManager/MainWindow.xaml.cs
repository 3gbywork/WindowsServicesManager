using System.Windows;
using System.ServiceProcess;
using System.Collections.Generic;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Data;
using System.Threading;
using System.Configuration;
using System.IO;

namespace WindowsServicesManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ServiceController[] allServices;
        List<ServiceStruct> allServicesList;

        Configuration cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        string[] filter;
        bool isFilterChanged = false;
        bool isConfigFileExist = true;

        public MainWindow()
        {
            InitializeComponent();

            btnRestart.IsEnabled = false;
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = false;

            GetFilterArgs();
            GetAllServices();
        }

        private void GetFilterArgs()
        {
            if (isConfigFileExist)
            {
                ConfigurationManager.RefreshSection("appSettings");
                txtFilter.Text = ConfigurationManager.AppSettings["FILTER"];
            }

            filter = txtFilter.Text.Split(';');
        }

        private void GetAllServices()
        {
            allServices = ServiceController.GetServices();
            allServicesList = new List<ServiceStruct>();

            foreach (ServiceController sc in allServices)
            {
                if (btnFilter.IsChecked == true)
                {
                    int isContains = 0;
                    foreach (string arg in filter)
                    {
                        if (string.IsNullOrWhiteSpace(arg)) continue;
                        if (sc.DisplayName.ToLower().Contains(arg))
                            isContains++;
                    }
                    if (isContains == 0) continue;
                }

                ServiceStruct ss = new ServiceStruct();

                ss.name = sc.ServiceName;
                ss.status = sc.Status;

                ss.Name = sc.DisplayName;
                ss.Status = ss.GetDisplayName(sc.Status);

                allServicesList.Add(ss);
            }

            lv.ItemsSource = allServicesList;
            txtNum.Text = "服务个数：" + allServicesList.Count.ToString();
        }

        struct ServiceStruct
        {
            public string name { get; set; }
            public ServiceControllerStatus status { get; set; }
            public string Name { get; set; }
            public string Status { get; set; }
            public string GetDisplayName(ServiceControllerStatus scs)
            {
                //Stopped
                string name = "已停止";

                if (scs.ToString().Equals("Running"))
                {
                    name = "已运行";
                }

                return name;
            }
        }

        private void lv_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lv.SelectedItem == null)
            {
                btnStart.IsEnabled = false;
                btnStop.IsEnabled = false;
                btnRestart.IsEnabled = false;

                return;
            }

            ServiceStruct ss = (ServiceStruct)lv.SelectedItem;

            if (ss.status.ToString().Equals("Running"))
            {
                btnStart.IsEnabled = false;
                btnStop.IsEnabled = true;
                btnRestart.IsEnabled = true;
            }
            else
            {
                btnStart.IsEnabled = true;
                btnStop.IsEnabled = false;
                btnRestart.IsEnabled = false;
            }
        }

        private void btnRestart_Click(object sender, RoutedEventArgs e)
        {
            ServiceStruct ss = (ServiceStruct)lv.SelectedItem;

            ServiceController sc = new ServiceController(ss.name);
            sc.Stop();
            sc.WaitForStatus(ServiceControllerStatus.Stopped);
            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running);
            sc.Close();

            GetAllServices();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            ServiceStruct ss = (ServiceStruct)lv.SelectedItem;

            ServiceController sc = new ServiceController(ss.name);
            sc.Stop();
            sc.WaitForStatus(ServiceControllerStatus.Stopped);
            sc.Close();

            GetAllServices();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            ServiceStruct ss = (ServiceStruct)lv.SelectedItem;

            ServiceController sc = new ServiceController(ss.name);
            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running);
            sc.Close();

            GetAllServices();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            GetFilterArgs();
            GetAllServices();
        }

        private void btnFilter_Click(object sender, RoutedEventArgs e)
        {
            if (btnFilter.IsChecked == true)
            {
                btnFilter.Content = "禁用过滤";
            }
            else
            {
                btnFilter.Content = "启用过滤";
            }

            GetFilterArgs();
            GetAllServices();
        }

        private void txtFilter_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            if (!isFilterChanged) return;

            if (File.Exists(cfg.FilePath))
            {
                isConfigFileExist = true;

                cfg.AppSettings.Settings["FILTER"].Value = txtFilter.Text;
                cfg.Save();
            }
            else
            {
                isConfigFileExist = false;
            }

            isFilterChanged = false;
        }

        private void txtFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            isFilterChanged = true;
        }

        private void dg_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            lv_SelectionChanged(sender, null);
        }

    }
}
