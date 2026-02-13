using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using NameParser.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NameParser.UI.ViewModels
{
    public class DatabaseBackupViewModel : ViewModelBase
    {
        private string _statusMessage;
        private bool _isProcessing;
        private string _backupLocation;
        private string _selectedBackupFile;
        private string _databaseInfo;

        public DatabaseBackupViewModel()
        {
            BackupFiles = new ObservableCollection<BackupFileInfo>();

            // Commands
            BrowseBackupLocationCommand = new RelayCommand(ExecuteBrowseBackupLocation);
            CreateBackupCommand = new RelayCommand(ExecuteCreateBackup, CanExecuteBackup);
            RestoreBackupCommand = new RelayCommand(ExecuteRestoreBackup, CanExecuteRestore);
            RefreshBackupListCommand = new RelayCommand(ExecuteRefreshBackupList);
            DeleteBackupCommand = new RelayCommand(ExecuteDeleteBackup, CanExecuteDeleteBackup);
            OpenBackupFolderCommand = new RelayCommand(ExecuteOpenBackupFolder);

            // Set default backup location
            BackupLocation = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ClubChallenger_Backups");

            LoadDatabaseInfo();
            RefreshBackupList();
        }

        public ObservableCollection<BackupFileInfo> BackupFiles { get; }

        public string BackupLocation
        {
            get => _backupLocation;
            set
            {
                if (SetProperty(ref _backupLocation, value))
                {
                    ((RelayCommand)CreateBackupCommand).RaiseCanExecuteChanged();
                    RefreshBackupList();
                }
            }
        }

        public string SelectedBackupFile
        {
            get => _selectedBackupFile;
            set
            {
                if (SetProperty(ref _selectedBackupFile, value))
                {
                    ((RelayCommand)RestoreBackupCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)DeleteBackupCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                SetProperty(ref _isProcessing, value);
                ((RelayCommand)CreateBackupCommand).RaiseCanExecuteChanged();
                ((RelayCommand)RestoreBackupCommand).RaiseCanExecuteChanged();
            }
        }

        public string DatabaseInfo
        {
            get => _databaseInfo;
            set => SetProperty(ref _databaseInfo, value);
        }

        public ICommand BrowseBackupLocationCommand { get; }
        public ICommand CreateBackupCommand { get; }
        public ICommand RestoreBackupCommand { get; }
        public ICommand RefreshBackupListCommand { get; }
        public ICommand DeleteBackupCommand { get; }
        public ICommand OpenBackupFolderCommand { get; }

        private void LoadDatabaseInfo()
        {
            try
            {
                using (var context = new RaceManagementContext())
                {
                    var connectionString = context.Database.GetDbConnection().ConnectionString;
                    var builder = new SqlConnectionStringBuilder(connectionString);
                    
                    var dbName = builder.InitialCatalog;
                    var serverName = builder.DataSource;

                    // Get database size
                    var sizeQuery = "SELECT SUM(size) * 8 / 1024.0 AS SizeMB FROM sys.database_files";
                    var command = context.Database.GetDbConnection().CreateCommand();
                    command.CommandText = sizeQuery;
                    context.Database.OpenConnection();
                    var sizeMB = command.ExecuteScalar();
                    context.Database.CloseConnection();

                    DatabaseInfo = $"Database: {dbName}\n" +
                                  $"Server: {serverName}\n" +
                                  $"Size: {sizeMB:F2} MB";
                }
            }
            catch (Exception ex)
            {
                DatabaseInfo = $"Error loading database info: {ex.Message}";
            }
        }

        private void ExecuteBrowseBackupLocation(object parameter)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Backup Location",
                ShowNewFolderButton = true,
                SelectedPath = BackupLocation
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                BackupLocation = dialog.SelectedPath;
                StatusMessage = $"Backup location changed to: {BackupLocation}";
            }
        }

        private bool CanExecuteBackup(object parameter)
        {
            return !IsProcessing && !string.IsNullOrWhiteSpace(BackupLocation);
        }

        private void ExecuteCreateBackup(object parameter)
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "Creating backup...";

                // Ensure backup directory exists
                if (!Directory.Exists(BackupLocation))
                {
                    Directory.CreateDirectory(BackupLocation);
                }

                // Generate backup filename with timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"RaceManagementDb_Backup_{timestamp}.bak";
                var backupPath = Path.Combine(BackupLocation, backupFileName);

                // Perform SQL Server backup
                using (var context = new RaceManagementContext())
                {
                    var connectionString = context.Database.GetDbConnection().ConnectionString;
                    var builder = new SqlConnectionStringBuilder(connectionString);
                    var databaseName = builder.InitialCatalog;

                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        
                        var backupCommand = $@"
                            BACKUP DATABASE [{databaseName}] 
                            TO DISK = @BackupPath 
                            WITH FORMAT, 
                                 INIT,
                                 NAME = 'Full Database Backup',
                                 SKIP,
                                 NOREWIND,
                                 NOUNLOAD,
                                 STATS = 10";

                        using (var command = new SqlCommand(backupCommand, connection))
                        {
                            command.CommandTimeout = 300; // 5 minutes timeout
                            command.Parameters.AddWithValue("@BackupPath", backupPath);
                            command.ExecuteNonQuery();
                        }
                    }
                }

                RefreshBackupList();
                StatusMessage = $"✅ Backup created successfully: {backupFileName}";
                MessageBox.Show($"Backup created successfully!\n\nLocation: {backupPath}", 
                    "Backup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error creating backup: {ex.Message}";
                MessageBox.Show($"Error creating backup:\n\n{ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private bool CanExecuteRestore(object parameter)
        {
            return !IsProcessing && !string.IsNullOrWhiteSpace(SelectedBackupFile) && File.Exists(SelectedBackupFile);
        }

        private void ExecuteRestoreBackup(object parameter)
        {
            var result = MessageBox.Show(
                "⚠️ WARNING: Restoring a backup will replace the current database!\n\n" +
                "A backup of the current database will be created automatically before restore.\n\n" +
                "Do you want to continue?",
                "Confirm Restore",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                IsProcessing = true;
                StatusMessage = "Creating automatic backup before restore...";

                // Create automatic backup before restore
                ExecuteCreateBackup(null);

                StatusMessage = "Restoring database from backup...";

                using (var context = new RaceManagementContext())
                {
                    var connectionString = context.Database.GetDbConnection().ConnectionString;
                    var builder = new SqlConnectionStringBuilder(connectionString);
                    var databaseName = builder.InitialCatalog;

                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        // Set database to single user mode to disconnect all users
                        var setSingleUserCommand = $@"
                            ALTER DATABASE [{databaseName}] 
                            SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
                        
                        using (var command = new SqlCommand(setSingleUserCommand, connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        try
                        {
                            // Restore database
                            var restoreCommand = $@"
                                RESTORE DATABASE [{databaseName}] 
                                FROM DISK = @BackupPath 
                                WITH REPLACE,
                                     STATS = 10";

                            using (var command = new SqlCommand(restoreCommand, connection))
                            {
                                command.CommandTimeout = 300; // 5 minutes timeout
                                command.Parameters.AddWithValue("@BackupPath", SelectedBackupFile);
                                command.ExecuteNonQuery();
                            }
                        }
                        finally
                        {
                            // Set database back to multi user mode
                            var setMultiUserCommand = $@"
                                ALTER DATABASE [{databaseName}] 
                                SET MULTI_USER";
                            
                            using (var command = new SqlCommand(setMultiUserCommand, connection))
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }

                LoadDatabaseInfo();
                StatusMessage = $"✅ Database restored successfully from: {Path.GetFileName(SelectedBackupFile)}";
                MessageBox.Show(
                    "Database restored successfully!\n\n" +
                    "The application will reload to reflect the restored data.",
                    "Restore Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Suggest restarting the application
                var restartResult = MessageBox.Show(
                    "It is recommended to restart the application after a restore.\n\n" +
                    "Would you like to restart now?",
                    "Restart Application",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (restartResult == MessageBoxResult.Yes)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        System.Diagnostics.Process.Start(Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location);
                        System.Windows.Application.Current.Shutdown();
                    });
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error restoring backup: {ex.Message}";
                MessageBox.Show($"Error restoring backup:\n\n{ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void ExecuteRefreshBackupList(object parameter)
        {
            RefreshBackupList();
        }

        private void RefreshBackupList()
        {
            try
            {
                BackupFiles.Clear();

                if (!Directory.Exists(BackupLocation))
                {
                    StatusMessage = "Backup location does not exist. Create a backup to initialize.";
                    return;
                }

                var backupFiles = Directory.GetFiles(BackupLocation, "*.bak")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .Select(f => new BackupFileInfo
                    {
                        FileName = f.Name,
                        FullPath = f.FullName,
                        Size = FormatFileSize(f.Length),
                        CreatedDate = f.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        Age = GetFileAge(f.LastWriteTime)
                    })
                    .ToList();

                foreach (var backup in backupFiles)
                {
                    BackupFiles.Add(backup);
                }

                if (BackupFiles.Count == 0)
                {
                    StatusMessage = "No backup files found in the selected location.";
                }
                else
                {
                    StatusMessage = $"Found {BackupFiles.Count} backup file(s).";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading backup list: {ex.Message}";
            }
        }

        private bool CanExecuteDeleteBackup(object parameter)
        {
            return !string.IsNullOrWhiteSpace(SelectedBackupFile) && File.Exists(SelectedBackupFile);
        }

        private void ExecuteDeleteBackup(object parameter)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete this backup?\n\n{Path.GetFileName(SelectedBackupFile)}",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    File.Delete(SelectedBackupFile);
                    RefreshBackupList();
                    SelectedBackupFile = null;
                    StatusMessage = "Backup file deleted successfully.";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error deleting backup: {ex.Message}";
                    MessageBox.Show($"Error deleting backup:\n\n{ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteOpenBackupFolder(object parameter)
        {
            try
            {
                if (!Directory.Exists(BackupLocation))
                {
                    Directory.CreateDirectory(BackupLocation);
                }

                System.Diagnostics.Process.Start("explorer.exe", BackupLocation);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening folder:\n\n{ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private string GetFileAge(DateTime fileDate)
        {
            var age = DateTime.Now - fileDate;
            if (age.TotalDays < 1)
                return $"{age.Hours}h ago";
            else if (age.TotalDays < 7)
                return $"{(int)age.TotalDays}d ago";
            else if (age.TotalDays < 30)
                return $"{(int)(age.TotalDays / 7)}w ago";
            else
                return $"{(int)(age.TotalDays / 30)}mo ago";
        }
    }

    public class BackupFileInfo
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public string Size { get; set; }
        public string CreatedDate { get; set; }
        public string Age { get; set; }
    }
}
