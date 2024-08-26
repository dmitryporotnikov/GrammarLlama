using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GrammarLlama
{
    /// <summary>
    /// Main window of the GrammarLlama application.
    /// Handles user interactions, chat management, and settings.
    /// </summary>
    public partial class MainWindow : Window
    {
        private OllamaApiClient _apiClient;
        private Message _currentAiMessage;
        private ObservableCollection<ArchivedChat> _archivedChats = new ObservableCollection<ArchivedChat>();
        public ObservableCollection<Message> CurrentChat { get; set; } = new ObservableCollection<Message>();

        /// <summary>
        /// Initializes the main window and sets up initial state.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            _apiClient = new OllamaApiClient();
            LoadSettings();
            MessageList.ItemsSource = CurrentChat;
            ConversationList.ItemsSource = _archivedChats;
        }

        /// <summary>
        /// Loads user settings from application settings and applies them.
        /// </summary>
        private void LoadSettings()
        {
            ApiEndpointTextBox.Text = Properties.Settings.Default.apiendpoint;
            ModelTextBox.Text = Properties.Settings.Default.model;
            SystemTextBox.Text = Properties.Settings.Default.system;
            ThemeComboBox.SelectedIndex = Properties.Settings.Default.isDarkTheme ? 1 : 0;

            _apiClient.UpdateSettings(ApiEndpointTextBox.Text, ModelTextBox.Text, SystemTextBox.Text);
            ApplyTheme(Properties.Settings.Default.isDarkTheme);
        }

        /// <summary>
        /// Toggles the visibility of the settings popup.
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPopup.IsOpen = !SettingsPopup.IsOpen;
        }

        /// <summary>
        /// Saves the current settings and applies them.
        /// </summary>
        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            string apiEndpoint = ApiEndpointTextBox.Text;
            string model = ModelTextBox.Text;
            string system = SystemTextBox.Text;
            bool isDarkTheme = ThemeComboBox.SelectedIndex == 1;

            _apiClient.UpdateSettings(apiEndpoint, model, system);

            // Save settings to application settings
            Properties.Settings.Default.apiendpoint = apiEndpoint;
            Properties.Settings.Default.model = model;
            Properties.Settings.Default.system = system;
            Properties.Settings.Default.isDarkTheme = isDarkTheme;
            Properties.Settings.Default.Save();

            ApplyTheme(isDarkTheme);

            MessageBox.Show("Settings saved!");
            SettingsPopup.IsOpen = false;
        }

        /// <summary>
        /// Applies the selected theme (light or dark) to the application.
        /// </summary>
        /// <param name="isDarkTheme">True if dark theme should be applied, false for light theme.</param>
        private void ApplyTheme(bool isDarkTheme)
        {
            Uri themeUri = new Uri(isDarkTheme ? "/Themes/DarkTheme.xaml" : "/Themes/LightTheme.xaml", UriKind.Relative);
            ResourceDictionary theme = new ResourceDictionary() { Source = themeUri };

            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(theme);

            // Force update of open popups
            if (SettingsPopup.IsOpen)
            {
                SettingsPopup.IsOpen = false;
                SettingsPopup.IsOpen = true;
            }
        }

        /// <summary>
        /// Event handler for the send message button click.
        /// </summary>
        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

        /// <summary>
        /// Copies the AI response to the clipboard when the copy button is clicked.
        /// </summary>
        private void CopyResponse_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Message message && !message.IsUser)
            {
                try
                {
                    Clipboard.SetText(message.Content);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to copy response: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Sends the user's message and processes the AI's response.
        /// </summary>
        private async Task SendMessage()
        {
            string userInput = InputTextBox.Text;
            if (string.IsNullOrWhiteSpace(userInput))
                return;

            AddMessage(userInput, isUser: true);
            InputTextBox.IsEnabled = false;
            SendButton.IsEnabled = false;

            // Add a "thinking" message
            _currentAiMessage = new Message { IsUser = false, Time = DateTime.Now.ToString("HH:mm"), Content = "Thinking..." };
            CurrentChat.Add(_currentAiMessage);

            try
            {
                string fullResponse = await _apiClient.SendMessageAsync(userInput);

                // Replace "Thinking..." with the actual response
                _currentAiMessage.Content = fullResponse;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Remove the "Thinking..." message if an error occurs
                CurrentChat.Remove(_currentAiMessage);
            }
            finally
            {
                InputTextBox.IsEnabled = true;
                SendButton.IsEnabled = true;
                InputTextBox.Clear();
                MessageListScrollViewer.ScrollToBottom();
            }
        }

        /// <summary>
        /// Initializes additional components after the main initialization.
        /// </summary>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            InputTextBox.KeyDown += InputTextBox_KeyDown;
        }

        /// <summary>
        /// Handles the key down event for the input text box.
        /// Allows sending a message by pressing Ctrl+Enter.
        /// </summary>
        private async void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                await SendMessage();
            }
        }

        /// <summary>
        /// Updates the UI with the streaming response from the AI.
        /// </summary>
        /// <param name="partialResponse">The partial response received from the AI.</param>
        private void UpdateStreamingResponse(string partialResponse)
        {
            Dispatcher.Invoke(() =>
            {
                _currentAiMessage.Content += partialResponse;
                MessageListScrollViewer.ScrollToBottom();
            });
        }

        /// <summary>
        /// Handles the completion of the AI's response.
        /// </summary>
        private void OnResponseCompleted()
        {
            Dispatcher.Invoke(() =>
            {
                MessageListScrollViewer.ScrollToBottom();
            });
        }

        /// <summary>
        /// Adds a new message to the current chat.
        /// </summary>
        /// <param name="content">The content of the message.</param>
        /// <param name="isUser">True if the message is from the user, false if from AI.</param>
        private void AddMessage(string content, bool isUser)
        {
            var message = new Message
            {
                Content = content,
                Time = DateTime.Now.ToString("HH:mm"),
                IsUser = isUser
            };
            CurrentChat.Add(message);
            MessageListScrollViewer.ScrollToBottom();
        }

        /// <summary>
        /// Handles the creation of a new chat.
        /// Archives the current chat if it's not empty.
        /// </summary>
        private void New_Chat_Click(object sender, RoutedEventArgs e)
        {
            // Archive the current chat
            if (CurrentChat.Count > 0)
            {
                var archivedChat = new ArchivedChat
                {
                    Timestamp = DateTime.Now,
                    LastMessage = CurrentChat.Last().Content,
                    Messages = new List<Message>(CurrentChat)
                };
                _archivedChats.Add(archivedChat);
            }

            // Clear the current chat
            CurrentChat.Clear();
        }

        /// <summary>
        /// Handles the selection of an archived conversation.
        /// Loads the selected chat and removes it from the archived list.
        /// </summary>
        private void ConversationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ConversationList.SelectedItem is ArchivedChat selectedChat)
            {
                // Save the current chat if it's not empty
                if (CurrentChat.Count > 0)
                {
                    var currentArchivedChat = new ArchivedChat
                    {
                        Timestamp = DateTime.Now,
                        LastMessage = CurrentChat.Last().Content,
                        Messages = new List<Message>(CurrentChat)
                    };
                    _archivedChats.Add(currentArchivedChat);
                }

                // Load the selected chat
                CurrentChat.Clear();
                foreach (var message in selectedChat.Messages)
                {
                    CurrentChat.Add(message);
                }

                // Remove the selected chat from the archived list
                _archivedChats.Remove(selectedChat);
            }
        }
    }

    /// <summary>
    /// Represents a single message in the chat.
    /// </summary>
    public class Message : INotifyPropertyChanged
    {
        private string _content = string.Empty;
        public string Content
        {
            get => _content;
            set
            {
                if (_content != value)
                {
                    _content = value;
                    OnPropertyChanged(nameof(Content));
                }
            }
        }
        public string Time { get; set; }
        public bool IsUser { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents an archived chat conversation.
    /// </summary>
    public class ArchivedChat : INotifyPropertyChanged
    {
        public DateTime Timestamp { get; set; }
        public string LastMessage { get; set; }
        public List<Message> Messages { get; set; }

        public string DisplayName => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


}