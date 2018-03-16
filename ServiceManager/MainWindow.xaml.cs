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
using System.ServiceProcess;
using System.Collections;
using System.Diagnostics;
using System.ComponentModel;
using ServiceManager.Dialogs;
using ServiceManager.Classes;

namespace ServiceManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<ServiceController> services;

        private List<Group> groups;

        private List<ListSortDirection?> sorting;

        public List<Group> Groups { get { return groups; } set { groups = value; } }        

        public MainWindow()
        {
            InitializeComponent();

            //this.DataContext = this;

            services = ServiceController.GetServices().ToList().OrderBy(x => x.ServiceName).ToList();

            sorting = new List<ListSortDirection?>
            {
                ListSortDirection.Ascending,
                null,
                null,
                null,
                null
            };

            //LVServices.ItemsSource = services.OrderBy(x => x.ServiceName);

            DGServices.Columns[0].SortDirection = ListSortDirection.Ascending;

            DGServices.ItemsSource = services;
        }

        private void DGServices_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            DataGrid grid = ((DataGrid)sender);

            MICreateGroup.IsEnabled = false;
            MIAddToGroup.IsEnabled = false;

            MIContinueServices.IsEnabled = false;
            MIStartServices.IsEnabled = false;
            MIStopServices.IsEnabled = false;
            MIPauseServices.IsEnabled = false;

            MIAutomaticStart.IsEnabled = false;
            MIDisabledStart.IsEnabled = false;
            MIManualStart.IsEnabled = false;

            foreach (ServiceController service in grid.SelectedItems)
            {
                switch (service.Status)
                {
                    case ServiceControllerStatus.Paused /*| ServiceControllerStatus.ContinuePending*/ :
                        MIContinueServices.IsEnabled = true;
                        //MIStartServices.IsEnabled = true;
                        MIStopServices.IsEnabled = true;
                        break;
                    case ServiceControllerStatus.Running /*| ServiceControllerStatus.StopPending*/ :
                        //MIContinueServices.IsEnabled = true;
                        //MIStartServices.IsEnabled = true;
                        MIStopServices.IsEnabled = true;
                        MIPauseServices.IsEnabled = true;
                        break;
                    case ServiceControllerStatus.Stopped /*| ServiceControllerStatus.StartPending*/ :
                        //MIContinueServices.IsEnabled = true;
                        MIStartServices.IsEnabled = true;
                        //MIStopServices.IsEnabled = true;
                        //MIPauseServices.IsEnabled = true;
                        break;
                }

                switch (service.StartType)
                {
                    case ServiceStartMode.Automatic:
                        //MIAutomaticStart.IsEnabled = true;
                        MIDisabledStart.IsEnabled = true;
                        MIManualStart.IsEnabled = true;
                        break;
                    case ServiceStartMode.Disabled:
                        MIAutomaticStart.IsEnabled = true;
                        //MIDisabledStart.IsEnabled = true;
                        MIManualStart.IsEnabled = true;
                        break;
                    case ServiceStartMode.Manual:
                        MIAutomaticStart.IsEnabled = true;
                        MIDisabledStart.IsEnabled = true;
                        //MIManualStart.IsEnabled = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Event solving the creation of new group
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateGroup_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EditGroupDialog();
            dialog.Title = "Create new group";
            if (dialog.ShowDialog() == true)
            {
                //MessageBox.Show("You said: " + dialog.ResponseText);

                var group = new Group(dialog.ResponseText);
            }
        }

        private void AddToGroup_Click(object sender, RoutedEventArgs e)
        {

        }

        private void StartServices_Click(object sender, RoutedEventArgs e)
        {
            foreach (ServiceController service in DGServices.SelectedItems)
            {
                if (service.Status != ServiceControllerStatus.Running)
                {
                    service.Start();
                    Debug.WriteLine(service.Status);
                    service.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 1, 0));
                }
            }
            DGServices.ItemsSource = null;
            DGServices.ItemsSource = services;
        }

        private void StopServices_Click(object sender, RoutedEventArgs e)
        {
            foreach (ServiceController service in DGServices.SelectedItems)
            {
                if (service.Status != ServiceControllerStatus.Stopped)
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 1, 0));
                }
            }
            DGServices.ItemsSource = null;
            DGServices.ItemsSource = services;
        }

        private void ContinueServices_Click(object sender, RoutedEventArgs e)
        {
            foreach (ServiceController service in DGServices.SelectedItems)
            {
                if (service.Status != ServiceControllerStatus.Running)
                {
                    service.Continue();
                    service.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 1, 0));
                }
            }
            DGServices.ItemsSource = null;
            DGServices.ItemsSource = services;
        }

        private void PauseServices_Click(object sender, RoutedEventArgs e)
        {
            foreach (ServiceController service in DGServices.SelectedItems)
            {
                if (service.Status != ServiceControllerStatus.Paused)
                {
                    service.Pause();
                    service.WaitForStatus(ServiceControllerStatus.Paused, new TimeSpan(0, 1, 0));
                }
            }
            DGServices.ItemsSource = null;
            DGServices.ItemsSource = services;
        }

        private void AutomaticStart_Click(object sender, RoutedEventArgs e)
        {
            foreach (ServiceController service in DGServices.SelectedItems)
            {
                if (service.StartType != ServiceStartMode.Automatic)
                {
                    //https://msdn.microsoft.com/en-us/library/windows/desktop/ms681987(v=vs.85).aspx
                    var xxx = PInvoke.NativeMethods.ChangeServiceConfigW(service.ServiceHandle.DangerousGetHandle(), 0xffffffff, 0x00000002, 0xffffffff, null, null, IntPtr.Zero, null, null, null, null);
                }
            }
            DGServices.ItemsSource = null;
            DGServices.ItemsSource = services;
        }

        private void ManualStart_Click(object sender, RoutedEventArgs e)
        {
            foreach (ServiceController service in DGServices.SelectedItems)
            {
                if (service.StartType != ServiceStartMode.Manual)
                {
                    //https://msdn.microsoft.com/en-us/library/windows/desktop/ms681987(v=vs.85).aspx
                    var xxx = PInvoke.NativeMethods.ChangeServiceConfigW(service.ServiceHandle.DangerousGetHandle(), 0xffffffff, 0x00000003, 0xffffffff, null, null, IntPtr.Zero, null, null, null, null);
                }
            }
            DGServices.ItemsSource = null;
            DGServices.ItemsSource = services;
        }

        private void DisabledStart_Click(object sender, RoutedEventArgs e)
        {
            foreach (ServiceController service in DGServices.SelectedItems)
            {
                if (service.StartType != ServiceStartMode.Disabled)
                {
                    //https://msdn.microsoft.com/en-us/library/windows/desktop/ms681987(v=vs.85).aspx
                    var xxx = PInvoke.NativeMethods.ChangeServiceConfigW(service.ServiceHandle.DangerousGetHandle(), 0xffffffff, 0x00000004, 0xffffffff, null, null, IntPtr.Zero, null, null, null, null);
                }
            }

            DGServices.ItemsSource = null;
            DGServices.ItemsSource = services;
        }

        /// <summary>
        /// Event for sort the list
        /// Unfortunately the is a problem with non string values. They are sorted incorectly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DGServices_Sorting(object sender, DataGridSortingEventArgs e)
        {
            //multiple orderby 
            //https://stackoverflow.com/questions/3047455/linq-orderby-with-more-than-one-field
            //orderby().thenby()

            var index = 0;

            //for all columns from datagrid
            for (var i = 0; i < DGServices.Columns.Count; i++)
            {
                //select column
                var column = DGServices.Columns[i];

                //if selected column is not clicked
                if (column != e.Column)
                    //set stored sorting for column
                    column.SortDirection = sorting[i];
                else
                {
                    index = i;

                    //set 
                    switch (column.SortDirection)
                    {
                        case ListSortDirection.Ascending:
                            sorting[i] = column.SortDirection = ListSortDirection.Descending;
                            break;

                        case ListSortDirection.Descending:
                            sorting[i] = column.SortDirection = null;
                            break;

                        default:
                            sorting[i] = column.SortDirection = ListSortDirection.Ascending;
                            break;
                    }
                }
            }

            List<ServiceController> ordered = (List<ServiceController>)DGServices.ItemsSource;

            if (sorting[index] != null)
            {
                switch (DGServices.Columns[index].SortMemberPath)
                {

                    case "ServiceName":
                        ordered = sortList(sorting[index].Value, ordered, x => x.ServiceName);
                        break;

                    case "DisplayName":
                        ordered = sortList(sorting[index].Value, ordered, x => x.DisplayName);
                        break;

                    case "StartType":
                        ordered = sortList(sorting[index].Value, ordered, x => x.StartType);
                        break;

                    case "Status":
                        ordered = sortList(sorting[index].Value, ordered, x => x.Status);
                        break;

                    case "ServiceType":
                        ordered = sortList(sorting[index].Value, ordered, x => x.ServiceType);
                        break;

                    //if (sorting[index].Value == ListSortDirection.Ascending)                    
                    //    ordered = ordered.OrderBy(x => DGServices.Columns[index].SortMemberPath).ToList();

                    //if (sorting[index].Value == ListSortDirection.Descending)
                    //    ordered = ordered.OrderByDescending(x => DGServices.Columns[index].SortMemberPath).ToList();

                    default:
                        break;

                }
            }

            DGServices.ItemsSource = ordered;

            for (var i = 0; i < DGServices.Columns.Count; i++)
            {
                DGServices.Columns[i].SortDirection = sorting[i];
            }

            e.Handled = true;
        }

        private List<ServiceController> sortList(ListSortDirection direction, List<ServiceController> list, Func<ServiceController, Object> keySelector)
        {
            if (direction == ListSortDirection.Ascending)
                list = list.OrderBy(keySelector).ToList();

            if (direction == ListSortDirection.Descending)
                list = list.OrderByDescending(keySelector).ToList();

            return list;
        }
    }
}
