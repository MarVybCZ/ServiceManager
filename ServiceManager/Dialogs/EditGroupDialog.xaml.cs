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
using System.Windows.Shapes;
using System.ServiceProcess;
using ServiceManager.Classes;
using System.Collections.ObjectModel;

namespace ServiceManager.Dialogs
{
    /// <summary>
    /// Interaction logic for EditGroupDialog.xaml
    /// </summary>
    public partial class EditGroupDialog : Window
    {
        private List<ServiceController> allServices;
        private ObservableCollection<ServiceController> availableServices;
        private ObservableCollection<ServiceWrapper> groupServices;
        private Group editingGroup;
        private bool isEditMode;

        public EditGroupDialog()
        {
            InitializeComponent();
            InitializeCollections();
            isEditMode = false;
        }

        public EditGroupDialog(Group groupToEdit, List<ServiceController> services) : this()
        {
            editingGroup = groupToEdit;
            allServices = services;
            isEditMode = true;
            
            SetupForEditing();
        }

        public EditGroupDialog(List<ServiceController> services) : this()
        {
            allServices = services;
            isEditMode = false;
            
            SetupForCreation();
        }

        private void InitializeCollections()
        {
            availableServices = new ObservableCollection<ServiceController>();
            groupServices = new ObservableCollection<ServiceWrapper>();
            
            AvailableServicesListBox.ItemsSource = availableServices;
            GroupServicesListBox.ItemsSource = groupServices;
            
            // Initialize placeholder visibility
            FilterPlaceholder.Visibility = Visibility.Visible;
        }

        private void SetupForEditing()
        {
            Title = "Edit Group";
            ResponseTextBox.Text = editingGroup.Name;
            
            // Add existing group services
            foreach (var service in editingGroup.Services)
            {
                groupServices.Add(service);
            }
            
            LoadAvailableServices();
            UpdateServiceCount();
        }

        private void SetupForCreation()
        {
            Title = "Create New Group";
            ResponseTextBox.Text = "";
            
            LoadAvailableServices();
            UpdateServiceCount();
        }

        private void LoadAvailableServices()
        {
            if (allServices == null) return;
            
            availableServices.Clear();
            
            // Get services that are not already in the group
            var groupServiceNames = groupServices.Select(s => s.ServiceName).ToHashSet();
            var filtered = allServices.Where(s => !groupServiceNames.Contains(s.ServiceName));
            
            // Apply text filter if any
            var filterText = FilterTextBox.Text?.ToLower() ?? "";
            if (!string.IsNullOrEmpty(filterText))
            {
                filtered = filtered.Where(s => 
                    s.ServiceName.ToLower().Contains(filterText) || 
                    s.DisplayName.ToLower().Contains(filterText));
            }
            
            foreach (var service in filtered.OrderBy(s => s.ServiceName))
            {
                availableServices.Add(service);
            }
        }

        private void UpdateServiceCount()
        {
            ServiceCountLabel.Text = $"{groupServices.Count} service{(groupServices.Count != 1 ? "s" : "")}";
        }

        public string ResponseText
        {
            get { return ResponseTextBox.Text; }
            set { ResponseTextBox.Text = value; }
        }

        public Group GetGroup()
        {
            if (isEditMode && editingGroup != null)
            {
                // Update existing group
                editingGroup.Name = ResponseTextBox.Text;
                editingGroup.Services.Clear();
                editingGroup.Services.AddRange(groupServices.ToList());
                return editingGroup;
            }
            else
            {
                // Create new group
                var group = new Group(ResponseTextBox.Text);
                group.Services.AddRange(groupServices.ToList());
                return group;
            }
        }

        private void AddService_Click(object sender, RoutedEventArgs e)
        {
            var selectedServices = AvailableServicesListBox.SelectedItems.Cast<ServiceController>().ToList();
            
            foreach (var service in selectedServices)
            {
                groupServices.Add(new ServiceWrapper(service));
                availableServices.Remove(service);
            }
            
            UpdateServiceCount();
        }

        private void RemoveService_Click(object sender, RoutedEventArgs e)
        {
            var selectedServices = GroupServicesListBox.SelectedItems.Cast<ServiceWrapper>().ToList();
            
            foreach (var serviceWrapper in selectedServices)
            {
                groupServices.Remove(serviceWrapper);
                
                // Find and add back to available services
                var serviceController = allServices.FirstOrDefault(s => s.ServiceName == serviceWrapper.ServiceName);
                if (serviceController != null)
                {
                    availableServices.Add(serviceController);
                }
            }
            
            // Re-sort available services
            var sortedServices = availableServices.OrderBy(s => s.ServiceName).ToList();
            availableServices.Clear();
            foreach (var service in sortedServices)
            {
                availableServices.Add(service);
            }
            
            UpdateServiceCount();
        }

        private void AddAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var service in availableServices.ToList())
            {
                groupServices.Add(new ServiceWrapper(service));
            }
            
            availableServices.Clear();
            UpdateServiceCount();
        }

        private void RemoveAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var serviceWrapper in groupServices.ToList())
            {
                var serviceController = allServices.FirstOrDefault(s => s.ServiceName == serviceWrapper.ServiceName);
                if (serviceController != null)
                {
                    availableServices.Add(serviceController);
                }
            }
            
            groupServices.Clear();
            
            // Re-sort available services
            var sortedServices = availableServices.OrderBy(s => s.ServiceName).ToList();
            availableServices.Clear();
            foreach (var service in sortedServices)
            {
                availableServices.Add(service);
            }
            
            UpdateServiceCount();
        }

        private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadAvailableServices();
            
            // Handle placeholder visibility
            FilterPlaceholder.Visibility = string.IsNullOrEmpty(FilterTextBox.Text) ? 
                Visibility.Visible : Visibility.Hidden;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ResponseTextBox.Text))
            {
                MessageBox.Show("Group name cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
