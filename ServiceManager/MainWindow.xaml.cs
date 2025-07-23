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
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using Newtonsoft.Json;
using System.Security.Principal;

namespace ServiceManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<ServiceController> services;
        private List<ListSortDirection?> sorting;

        public List<Group> Groups { get; } = new List<Group>();

        public MainWindow()
        {
            InitializeComponent();

            // Check permissions and show status
            CheckAndDisplayPermissionStatus();

            // Initialize services list
            InitializeServicesWithErrorHandling();

            sorting = new List<ListSortDirection?>
            {
                ListSortDirection.Ascending,
                null,
                null,
                null,
                null
            };

            DGServices.Columns[0].SortDirection = ListSortDirection.Ascending;
            LBGroups.ItemsSource = Groups;
        }

        private void CheckAndDisplayPermissionStatus()
        {
            if (!WindowsPermissionHelper.CanAccessWindowsServices())
            {
                MessageBox.Show("Unable to access Windows services. This application requires appropriate permissions to manage services.\n\nPlease run as administrator or check your system configuration.", 
                    "Service Access Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (!WindowsPermissionHelper.IsRunningAsAdministrator())
            {
                MessageBox.Show("This application requires administrator privileges to manage Windows services.\nSome operations may be limited without elevated permissions.\n\nPlease run as administrator for full functionality.", 
                    "Administrator Privileges Recommended", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void InitializeServicesWithErrorHandling()
        {
            try
            {
                services = ServiceController.GetServices().ToList().OrderBy(x => x.ServiceName).ToList();
                DGServices.ItemsSource = services;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load Windows services: {ex.Message}\n\nPlease check your permissions and try running as administrator.", 
                    "Service Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
                services = new List<ServiceController>(); // Initialize empty list to prevent crashes
                DGServices.ItemsSource = services;
            }
        }

        private bool IsRunningAsAdministrator()
        {
            return WindowsPermissionHelper.IsRunningAsAdministrator();
        }

        private void RefreshServicesList()
        {
            try
            {
                // Dispose old services to free resources
                if (services != null)
                {
                    foreach (var service in services)
                    {
                        try
                        {
                            service?.Dispose();
                        }
                        catch 
                        {
                            // Ignore disposal errors
                        }
                    }
                }

                // Get fresh services list
                services = ServiceController.GetServices().ToList().OrderBy(x => x.ServiceName).ToList();
                
                DGServices.ItemsSource = null;
                DGServices.ItemsSource = services;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing services list: {ex.Message}", 
                    "Refresh Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DGServices_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            DataGrid grid = ((DataGrid)sender);

            //MICreateGroup.IsEnabled = false;
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
            ShowCreateGroupDialog();

            LBGroups.ItemsSource = null;

            LBGroups.ItemsSource = Groups;
        }

        private void ShowCreateGroupDialog()
        {
            var dialog = new EditGroupDialog(services)
            {
                Title = "Create new group"
            };

            if (dialog.ShowDialog() == true)
            {
                if (dialog.ResponseText.Length == 0)
                {
                    MessageBox.Show("Group name cannot be empty");
                    return;
                }

                if (Groups.Where(x => x.Name == dialog.ResponseText).Count() > 0)
                {
                    MessageBox.Show("Group with this name already exists");
                    return;
                }

                var group = dialog.GetGroup();
                Groups.Add(group);
            }
        }

        private void EditGroup_Click(object sender, RoutedEventArgs e)
        {
            Group selectedGroup = LBGroups.SelectedItem as Group;
            
            if (selectedGroup == null)
            {
                MessageBox.Show("Please select a group to edit.", "No Group Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ShowEditGroupDialog(selectedGroup);
        }

        private void ShowEditGroupDialog(Group groupToEdit)
        {
            var dialog = new EditGroupDialog(groupToEdit, services);

            if (dialog.ShowDialog() == true)
            {
                if (string.IsNullOrWhiteSpace(dialog.ResponseText))
                {
                    MessageBox.Show("Group name cannot be empty");
                    return;
                }

                // Check if name conflicts with other groups (excluding the current one)
                if (Groups.Where(x => x.Name == dialog.ResponseText && x != groupToEdit).Count() > 0)
                {
                    MessageBox.Show("Group with this name already exists");
                    return;
                }

                // The group is already updated by the dialog
                LBGroups.Items.Refresh();
            }
        }

        private void LBGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // This could be used to update button states based on selection
        }

        private void StartGroupServices_Click(object sender, RoutedEventArgs e)
        {
            Group selectedGroup = LBGroups.SelectedItem as Group;
            
            if (selectedGroup == null)
            {
                MessageBox.Show("Please select a group.", "No Group Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ControlGroupServices(selectedGroup, ServiceAction.Start);
        }

        private void StopGroupServices_Click(object sender, RoutedEventArgs e)
        {
            Group selectedGroup = LBGroups.SelectedItem as Group;
            
            if (selectedGroup == null)
            {
                MessageBox.Show("Please select a group.", "No Group Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ControlGroupServices(selectedGroup, ServiceAction.Stop);
        }

        private void PauseGroupServices_Click(object sender, RoutedEventArgs e)
        {
            Group selectedGroup = LBGroups.SelectedItem as Group;
            
            if (selectedGroup == null)
            {
                MessageBox.Show("Please select a group.", "No Group Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ControlGroupServices(selectedGroup, ServiceAction.Pause);
        }

        private void ContinueGroupServices_Click(object sender, RoutedEventArgs e)
        {
            Group selectedGroup = LBGroups.SelectedItem as Group;
            
            if (selectedGroup == null)
            {
                MessageBox.Show("Please select a group.", "No Group Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ControlGroupServices(selectedGroup, ServiceAction.Continue);
        }

        private enum ServiceAction
        {
            Start,
            Stop,
            Pause,
            Continue
        }

        private void ControlGroupServices(Group group, ServiceAction action)
        {
            if (!IsRunningAsAdministrator())
            {
                MessageBox.Show($"Administrator privileges are required to {action.ToString().ToLower()} services.", 
                    "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (group.Services.Count == 0)
            {
                MessageBox.Show("The selected group contains no services.", "Empty Group", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var actionText = action.ToString().ToLower();
            var result = MessageBox.Show($"Do you want to {actionText} all {group.Services.Count} service(s) in group '{group.Name}'?", 
                $"Confirm {action}", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result != MessageBoxResult.Yes)
                return;

            int successCount = 0;
            int failCount = 0;
            var errors = new List<string>();

            foreach (var serviceWrapper in group.Services)
            {
                try
                {
                    var serviceController = services.FirstOrDefault(s => s.ServiceName == serviceWrapper.ServiceName);
                    if (serviceController == null)
                    {
                        errors.Add($"Service '{serviceWrapper.ServiceName}' not found.");
                        failCount++;
                        continue;
                    }

                    bool actionPerformed = false;

                    switch (action)
                    {
                        case ServiceAction.Start:
                            if (serviceController.Status != ServiceControllerStatus.Running)
                            {
                                serviceController.Start();
                                serviceController.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 1, 0));
                                actionPerformed = true;
                            }
                            break;
                        case ServiceAction.Stop:
                            if (serviceController.Status != ServiceControllerStatus.Stopped)
                            {
                                serviceController.Stop();
                                serviceController.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 1, 0));
                                actionPerformed = true;
                            }
                            break;
                        case ServiceAction.Pause:
                            if (serviceController.Status == ServiceControllerStatus.Running)
                            {
                                serviceController.Pause();
                                serviceController.WaitForStatus(ServiceControllerStatus.Paused, new TimeSpan(0, 1, 0));
                                actionPerformed = true;
                            }
                            break;
                        case ServiceAction.Continue:
                            if (serviceController.Status == ServiceControllerStatus.Paused)
                            {
                                serviceController.Continue();
                                serviceController.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 1, 0));
                                actionPerformed = true;
                            }
                            break;
                    }

                    if (actionPerformed)
                        successCount++;
                    else
                        failCount++;
                }
                catch (InvalidOperationException ex)
                {
                    errors.Add($"Cannot {actionText} service '{serviceWrapper.ServiceName}': {ex.Message}");
                    failCount++;
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    errors.Add($"Access denied {actionText}ing service '{serviceWrapper.ServiceName}': {ex.Message}");
                    failCount++;
                }
                catch (System.ServiceProcess.TimeoutException)
                {
                    errors.Add($"Service '{serviceWrapper.ServiceName}' failed to {actionText} within timeout period.");
                    failCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Error {actionText}ing service '{serviceWrapper.ServiceName}': {ex.Message}");
                    failCount++;
                }
            }

            // Show results
            var message = $"Group '{group.Name}' service control completed:\n" +
                         $"• Successfully {actionText}ed: {successCount} service(s)\n" +
                         $"• Failed: {failCount} service(s)";

            if (errors.Count > 0 && errors.Count <= 5)
            {
                message += "\n\nErrors:\n" + string.Join("\n", errors);
            }
            else if (errors.Count > 5)
            {
                message += $"\n\nErrors (showing first 5 of {errors.Count}):\n" + string.Join("\n", errors.Take(5));
            }

            var icon = failCount == 0 ? MessageBoxImage.Information : 
                      successCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Error;
            
            MessageBox.Show(message, $"Group Service Control Results", MessageBoxButton.OK, icon);

            // Refresh the services list to show updated statuses
            RefreshServicesList();
        }
        
        private void AddToGroup_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveGroup_Click(object sender, RoutedEventArgs e)
        {
            Group group = LBGroups.SelectedItem as Group;

            if (group != null)
                RemoveGroup(group);
        }

        private void RemoveGroup(Group group)
        {
            MessageBoxResult result = MessageBox.Show("Do you really want to remove group '" + group.Name + "'?", "Warning", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                Groups.Remove(group);

                LBGroups.Items.Refresh();                
            }
        }

        private void StartServices_Click(object sender, RoutedEventArgs e)
        {
            if (!IsRunningAsAdministrator())
            {
                MessageBox.Show("Administrator privileges are required to start services.", 
                    "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (ServiceController service in DGServices.SelectedItems)
            {
                try
                {
                    if (service.Status != ServiceControllerStatus.Running)
                    {
                        service.Start();
                        Debug.WriteLine(service.Status);
                        service.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 1, 0));
                    }
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show($"Cannot start service '{service.ServiceName}': {ex.Message}", 
                        "Service Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    MessageBox.Show($"Access denied starting service '{service.ServiceName}': {ex.Message}", 
                        "Permission Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (System.ServiceProcess.TimeoutException)
                {
                    MessageBox.Show($"Service '{service.ServiceName}' failed to start within timeout period.", 
                        "Timeout Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            RefreshServicesList();
        }

        private void StopServices_Click(object sender, RoutedEventArgs e)
        {
            if (!IsRunningAsAdministrator())
            {
                MessageBox.Show("Administrator privileges are required to stop services.", 
                    "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (ServiceController service in DGServices.SelectedItems)
            {
                try
                {
                    if (service.Status != ServiceControllerStatus.Stopped)
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 1, 0));
                    }
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show($"Cannot stop service '{service.ServiceName}': {ex.Message}", 
                        "Service Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    MessageBox.Show($"Access denied stopping service '{service.ServiceName}': {ex.Message}", 
                        "Permission Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (System.ServiceProcess.TimeoutException)
                {
                    MessageBox.Show($"Service '{service.ServiceName}' failed to stop within timeout period.", 
                        "Timeout Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            RefreshServicesList();
        }

        private void ContinueServices_Click(object sender, RoutedEventArgs e)
        {
            if (!IsRunningAsAdministrator())
            {
                MessageBox.Show("Administrator privileges are required to continue services.", 
                    "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (ServiceController service in DGServices.SelectedItems)
            {
                try
                {
                    if (service.Status != ServiceControllerStatus.Running)
                    {
                        service.Continue();
                        service.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 1, 0));
                    }
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show($"Cannot continue service '{service.ServiceName}': {ex.Message}", 
                        "Service Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    MessageBox.Show($"Access denied continuing service '{service.ServiceName}': {ex.Message}", 
                        "Permission Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (System.ServiceProcess.TimeoutException)
                {
                    MessageBox.Show($"Service '{service.ServiceName}' failed to continue within timeout period.", 
                        "Timeout Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            RefreshServicesList();
        }

        private void PauseServices_Click(object sender, RoutedEventArgs e)
        {
            if (!IsRunningAsAdministrator())
            {
                MessageBox.Show("Administrator privileges are required to pause services.", 
                    "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (ServiceController service in DGServices.SelectedItems)
            {
                try
                {
                    if (service.Status != ServiceControllerStatus.Paused)
                    {
                        service.Pause();
                        service.WaitForStatus(ServiceControllerStatus.Paused, new TimeSpan(0, 1, 0));
                    }
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show($"Cannot pause service '{service.ServiceName}': {ex.Message}", 
                        "Service Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    MessageBox.Show($"Access denied pausing service '{service.ServiceName}': {ex.Message}", 
                        "Permission Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (System.ServiceProcess.TimeoutException)
                {
                    MessageBox.Show($"Service '{service.ServiceName}' failed to pause within timeout period.", 
                        "Timeout Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            RefreshServicesList();
        }

        private void AutomaticStart_Click(object sender, RoutedEventArgs e)
        {
            if (!IsRunningAsAdministrator())
            {
                MessageBox.Show("Administrator privileges are required to change service startup type.", 
                    "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (ServiceController service in DGServices.SelectedItems)
            {
                try
                {
                    if (service.StartType != ServiceStartMode.Automatic)
                    {
                        //https://msdn.microsoft.com/en-us/library/windows/desktop/ms681987(v=vs.85).aspx
                        var result = PInvoke.NativeMethods.ChangeServiceConfigW(service.ServiceHandle.DangerousGetHandle(), 0xffffffff, 0x00000002, 0xffffffff, null, null, IntPtr.Zero, null, null, null, null);
                        if (!result)
                        {
                            MessageBox.Show($"Failed to change startup type for service '{service.ServiceName}'. Error code: {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}", 
                                "Service Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error changing startup type for service '{service.ServiceName}': {ex.Message}", 
                        "Service Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            RefreshServicesList();
        }

        private void ManualStart_Click(object sender, RoutedEventArgs e)
        {
            if (!IsRunningAsAdministrator())
            {
                MessageBox.Show("Administrator privileges are required to change service startup type.", 
                    "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (ServiceController service in DGServices.SelectedItems)
            {
                try
                {
                    if (service.StartType != ServiceStartMode.Manual)
                    {
                        //https://msdn.microsoft.com/en-us/library/windows/desktop/ms681987(v=vs.85).aspx
                        var result = PInvoke.NativeMethods.ChangeServiceConfigW(service.ServiceHandle.DangerousGetHandle(), 0xffffffff, 0x00000003, 0xffffffff, null, null, IntPtr.Zero, null, null, null, null);
                        if (!result)
                        {
                            MessageBox.Show($"Failed to change startup type for service '{service.ServiceName}'. Error code: {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}", 
                                "Service Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error changing startup type for service '{service.ServiceName}': {ex.Message}", 
                        "Service Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            RefreshServicesList();
        }

        private void DisabledStart_Click(object sender, RoutedEventArgs e)
        {
            if (!IsRunningAsAdministrator())
            {
                MessageBox.Show("Administrator privileges are required to change service startup type.", 
                    "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (ServiceController service in DGServices.SelectedItems)
            {
                try
                {
                    if (service.StartType != ServiceStartMode.Disabled)
                    {
                        //https://msdn.microsoft.com/en-us/library/windows/desktop/ms681987(v=vs.85).aspx
                        var result = PInvoke.NativeMethods.ChangeServiceConfigW(service.ServiceHandle.DangerousGetHandle(), 0xffffffff, 0x00000004, 0xffffffff, null, null, IntPtr.Zero, null, null, null, null);
                        if (!result)
                        {
                            MessageBox.Show($"Failed to change startup type for service '{service.ServiceName}'. Error code: {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}", 
                                "Service Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error changing startup type for service '{service.ServiceName}': {ex.Message}", 
                        "Service Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            RefreshServicesList();
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

        private void Window_Initialized(object sender, EventArgs e)
        {
            LoadGroups();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            SaveGroups();
            
            // Clean up service resources
            CleanupResources();
        }

        private void CleanupResources()
        {
            try
            {
                if (services != null)
                {
                    foreach (var service in services)
                    {
                        try
                        {
                            service?.Dispose();
                        }
                        catch
                        {
                            // Ignore disposal errors
                        }
                    }
                    services.Clear();
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        private void LoadGroups()
        {
            try
            {
                //    XmlSerializer serializer = new XmlSerializer(typeof(IEnumerable<Group>));                

                //    var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "//data.xml";

                //    if (File.Exists(path))
                //    {
                //        using (System.IO.FileStream file = System.IO.File.OpenRead(path))
                //        {
                //            var x = (List<Group>)serializer.Deserialize(file);

                //            Groups.AddRange(x);
                //            file.Close();
                //        }
                //    }               

                //var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "//data.json";

                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var appFolder = System.IO.Path.Combine(appDataPath, "ServiceManager");
                
                // Create app folder if it doesn't exist
                if (!Directory.Exists(appFolder))
                {
                    Directory.CreateDirectory(appFolder);
                }
                
                var path = System.IO.Path.Combine(appFolder, "data.json");

                if (File.Exists(path))
                {
                    Groups.AddRange(JsonConvert.DeserializeObject<List<Group>>(File.ReadAllText(path)));
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Unable to load data: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveGroups()
        {
            //try
            //{
            //    XmlSerializer serializer = new XmlSerializer(typeof(List<Group>));

            //    var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "//data.xml";
            //    using (System.IO.FileStream file = System.IO.File.Create(path))
            //    {
            //        serializer.Serialize(file, Groups);
            //        file.Close();
            //    }
            //}
            //catch (Exception e)
            //{
            //    MessageBox.Show("Unable to save data.", "Error");
            //}

            try
            {
                //var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "//data.json";

                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var appFolder = System.IO.Path.Combine(appDataPath, "ServiceManager");
                
                // Create app folder if it doesn't exist
                if (!Directory.Exists(appFolder))
                {
                    Directory.CreateDirectory(appFolder);
                }
                
                var path = System.IO.Path.Combine(appFolder, "data.json");

                var text = JsonConvert.SerializeObject(Groups);

                File.WriteAllText(path, text);
            }
            catch (Exception e) 
            { 
                MessageBox.Show($"Unable to save data: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning); 
            }
        }
    }
}
