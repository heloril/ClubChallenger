using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using NameParser.UI.ViewModels;
using NameParser.UI.Converters;

namespace NameParser.UI
{
    public partial class MainWindow : Window
    {
        private WebView2 _emailEditorWebView;
        private bool _isEditorReady = false;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize WebView2 for WYSIWYG editor
            InitializeEmailEditor();

            // Wire up for email preview updates
            if (DataContext is MainViewModel viewModel)
            {
                Loaded += (s, e) =>
                {
                    // Listen for changes in EmailBody (when template is generated)
                    viewModel.ChallengeMailingViewModel.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == nameof(viewModel.ChallengeMailingViewModel.EmailBody))
                        {
                            // Auto-load template into editor when generated
                            if (_isEditorReady)
                            {
                                LoadTemplateIntoEditor();
                            }
                        }
                    };
                };
            }
        }

        private async void InitializeEmailEditor()
        {
            try
            {
                _emailEditorWebView = new WebView2();
                WebViewContainer.Children.Add(_emailEditorWebView);

                // Initialize WebView2
                await _emailEditorWebView.EnsureCoreWebView2Async(null);

                // Handle messages from JavaScript
                _emailEditorWebView.CoreWebView2.WebMessageReceived += (sender, args) =>
                {
                    var message = System.Text.Json.JsonDocument.Parse(args.WebMessageAsJson);
                    var messageType = message.RootElement.GetProperty("type").GetString();

                    if (messageType == "editorReady")
                    {
                        _isEditorReady = true;
                        // Load template if already generated
                        LoadTemplateIntoEditor();
                    }
                    else if (messageType == "contentChanged")
                    {
                        // Update ViewModel with new HTML
                        var html = message.RootElement.GetProperty("html").GetString();
                        if (DataContext is MainViewModel viewModel)
                        {
                            viewModel.ChallengeMailingViewModel.EmailBody = html;
                        }
                    }
                };

                // Load the CKEditor HTML
                var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "CKEditor.html");

                if (File.Exists(htmlPath))
                {
                    _emailEditorWebView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
                }
                else
                {
                    // Fallback: Load from embedded HTML string
                    var editorHtml = GetCKEditorHtml();
                    _emailEditorWebView.NavigateToString(editorHtml);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing email editor: {ex.Message}\n\nPlease install Microsoft Edge WebView2 Runtime.", 
                    "Editor Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadTemplateIntoEditor()
        {
            if (!_isEditorReady || _emailEditorWebView == null) return;

            var viewModel = DataContext as MainViewModel;
            var html = viewModel?.ChallengeMailingViewModel?.EmailBody;

            if (!string.IsNullOrWhiteSpace(html))
            {
                // Escape HTML for JavaScript
                var escapedHtml = System.Text.Json.JsonSerializer.Serialize(html);
                await _emailEditorWebView.CoreWebView2.ExecuteScriptAsync($"setContent({escapedHtml});");
            }
        }

        private async void GetHtml_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditorReady || _emailEditorWebView == null) return;

            try
            {
                var result = await _emailEditorWebView.CoreWebView2.ExecuteScriptAsync("getContent();");
                var html = System.Text.Json.JsonSerializer.Deserialize<string>(result);

                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ChallengeMailingViewModel.EmailBody = html;
                }

                MessageBox.Show("HTML retrieved from editor and saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting HTML: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTemplate_Click(object sender, RoutedEventArgs e)
        {
            LoadTemplateIntoEditor();
        }

        private async void InsertTable_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditorReady || _emailEditorWebView == null) return;

            try
            {
                // Insert a default table (4 rows x 4 columns)
                await _emailEditorWebView.CoreWebView2.ExecuteScriptAsync("insertTable(4, 4);");
                MessageBox.Show("Table inserted! You can now edit it in the editor.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inserting table: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetCKEditorHtml()
        {
            // Fallback HTML embedded as string (file not found)
            return @"<!DOCTYPE html><html><head><meta charset='utf-8'><title>CKEditor</title><style>body{margin:0;padding:10px;font-family:'Segoe UI',Arial,sans-serif;background-color:#f5f5f5}#editor-container{background-color:white;padding:0;min-height:500px}.status-bar{background-color:#e3f2fd;padding:10px;margin-bottom:10px;border-radius:4px;font-size:12px;color:#1976d2}</style></head><body><div class='status-bar' id='statusBar'>📧 CKEditor ready</div><div id='editor-container'><textarea id='editor' name='editor'><h1>Email Editor Ready</h1><p>Click Load Template to start editing</p></textarea></div><script src='https://cdn.ckeditor.com/4.22.1/standard-all/ckeditor.js'></script><script>var editor;var isReady=false;CKEDITOR.replace('editor',{height:500,allowedContent:true,on:{instanceReady:function(evt){editor=evt.editor;isReady=true;if(window.chrome&&window.chrome.webview){window.chrome.webview.postMessage({type:'editorReady'})}},change:function(){if(isReady&&window.chrome&&window.chrome.webview){var html=editor.getData();window.chrome.webview.postMessage({type:'contentChanged',html:html})}}}});function setContent(html){if(editor&&html){editor.setData(html)}}function getContent(){if(editor){return editor.getData()}return ''}function insertTable(rows,cols){if(editor){var html='<table style=""border-collapse:collapse;width:100%;margin:10px 0""><thead><tr style=""background-color:#FF9800;color:white"">';for(var i=0;i<cols;i++)html+='<th style=""border:1px solid #ddd;padding:8px"">Header '+(i+1)+'</th>';html+='</tr></thead><tbody>';for(var r=0;r<rows-1;r++){html+='<tr'+(r%2===0?' style=""background-color:#f2f2f2""':'')+'>'; for(var c=0;c<cols;c++)html+='<td style=""border:1px solid #ddd;padding:8px"">Cell</td>';html+='</tr>'}html+='</tbody></table><p>&nbsp;</p>';editor.insertHtml(html)}}</script></body></html>";
        }

        private void RacesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                var dataGrid = sender as DataGrid;
                viewModel.SelectedRaces = dataGrid?.SelectedItems.Cast<object>().ToList();
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            // Open the context menu when the button is clicked
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        private void ChallengerExportButton_Click(object sender, RoutedEventArgs e)
        {
            // Open the context menu when the button is clicked
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }
    }
}
