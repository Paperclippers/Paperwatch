using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;
using TagLib;

namespace PaperwatchWPF
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer = null!; 
        private bool _isDraggingSlider = false;
        private bool _isPlaying = false;
        private bool _isFullscreen = false;
        private WindowState _previousState;
        private double _previousWidth, _previousHeight, _previousTop, _previousLeft;

        public string? InitialFile { get; set; }
        public int SkipInterval { get; set; } = 10;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTimer();
            this.Loaded += MainWindow_Loaded;
            this.KeyDown += MainWindow_KeyDown;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _isFullscreen)
            {
                ToggleFullscreen();
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(InitialFile))
            {
                PlayFile(InitialFile);
            }
        }

        private void PlayFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath)) return;
                if (!Path.IsPathRooted(filePath)) filePath = Path.GetFullPath(filePath);
                if (!System.IO.File.Exists(filePath))
                {
                    MessageBox.Show($"File not found: {filePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                UpdateMetadata(filePath);
                emptyStateText.Visibility = Visibility.Collapsed;
                _isPlaying = true;
                UpdatePlayPauseButton();

                mediaElement.Stop();
                mediaElement.Source = null;
                mediaElement.Source = new Uri(filePath, UriKind.Absolute);
                mediaElement.Volume = volumeSlider.Value;
                
                mediaElement.Play(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing playback: {ex.Message}", "Playback Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateMetadata(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            bool isAudio = extension == ".mp3" || extension == ".wav" || extension == ".flac" || extension == ".m4a";

            if (isAudio)
            {
                audioInfoPanel.Visibility = Visibility.Visible;
                try
                {
                    using (var file = TagLib.File.Create(filePath))
                    {
                        songTitleText.Text = !string.IsNullOrEmpty(file.Tag.Title) ? file.Tag.Title : Path.GetFileNameWithoutExtension(filePath);
                        artistNameText.Text = !string.IsNullOrEmpty(file.Tag.FirstPerformer) ? file.Tag.FirstPerformer : "Unknown Artist";

                        if (file.Tag.Pictures.Length > 0)
                        {
                            var bin = file.Tag.Pictures[0].Data.Data;
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = new MemoryStream(bin);
                            bitmap.EndInit();
                            albumArtImage.Source = bitmap;
                        }
                        else
                        {
                            albumArtImage.Source = null;
                        }
                    }
                }
                catch
                {
                    songTitleText.Text = Path.GetFileNameWithoutExtension(filePath);
                    artistNameText.Text = "Unknown Artist";
                    albumArtImage.Source = null;
                }
            }
            else
            {
                audioInfoPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(0.1);
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!_isDraggingSlider && mediaElement.NaturalDuration.HasTimeSpan)
            {
                timelineSlider.Value = mediaElement.Position.TotalSeconds;
                currentTimeText.Text = FormatTime(mediaElement.Position);
            }
        }

        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleFullscreen();
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void OpenMedia_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Media files (*.mp3;*.mp4;*.mkv;*.avi)|*.mp3;*.mp4;*.mkv;*.avi|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                PlayFile(openFileDialog.FileName);
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            settingsOverlay.Visibility = Visibility.Visible;
            skipIntervalBox.Text = SkipInterval.ToString();
        }

        private void CloseSettings_Click(object sender, RoutedEventArgs e)
        {
            settingsOverlay.Visibility = Visibility.Collapsed;
        }

        private void UpdateSkipInterval_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(skipIntervalBox.Text, out int result) && result > 0)
            {
                SkipInterval = result;
                MessageBox.Show($"Skip interval set to {result} seconds.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Please enter a valid positive number for the skip interval.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SetDefault_Click(object sender, RoutedEventArgs e)
        {
            FileAssociationHelper.SetAsDefault(".mp3", "Paperwatch.Media", "Paperwatch Audio File");
            FileAssociationHelper.SetAsDefault(".mp4", "Paperwatch.Video", "Paperwatch Video File");
            MessageBox.Show("Associations updated! You may need to restart Explorer or use 'Open with' -> 'Choose another app' one last time to confirm.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Fullscreen_Click(object sender, RoutedEventArgs e)
        {
            ToggleFullscreen();
        }

        private void mediaElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleFullscreen();
            }
        }

        private void ToggleFullscreen()
        {
            _isFullscreen = !_isFullscreen;

            if (_isFullscreen)
            {
                _previousState = WindowState;
                _previousWidth = Width;
                _previousHeight = Height;
                _previousTop = Top;
                _previousLeft = Left;

                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                ResizeMode = ResizeMode.NoResize;

                topBar.Visibility = Visibility.Collapsed;
                controlBar.Visibility = Visibility.Collapsed;
            }
            else
            {
                WindowState = _previousState;
                Width = _previousWidth;
                Height = _previousHeight;
                Top = _previousTop;
                Left = _previousLeft;
                ResizeMode = ResizeMode.CanResize;

                topBar.Visibility = Visibility.Visible;
                controlBar.Visibility = Visibility.Visible;
            }
            
            fullscreenButton.Foreground = _isFullscreen ? Brushes.SkyBlue : Brushes.LightGray;
            topFullscreenButton.Foreground = _isFullscreen ? Brushes.SkyBlue : Brushes.LightGray;
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source == null) return;

            if (_isPlaying)
            {
                mediaElement.Pause();
                _isPlaying = false;
            }
            else
            {
                mediaElement.Play();
                _isPlaying = true;
            }
            UpdatePlayPauseButton();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source == null) return;
            mediaElement.Position = mediaElement.Position.Subtract(TimeSpan.FromSeconds(SkipInterval));
            timelineSlider.Value = mediaElement.Position.TotalSeconds;
        }

        private void Skip_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source == null) return;
            mediaElement.Position = mediaElement.Position.Add(TimeSpan.FromSeconds(SkipInterval));
            timelineSlider.Value = mediaElement.Position.TotalSeconds;
        }

        private void UpdatePlayPauseButton()
        {
            playPauseButton.Content = _isPlaying ? "\uE769" : "\uE768";
            playPauseButton.ToolTip = _isPlaying ? "Pause" : "Play";
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            mediaElement.Stop();
            _isPlaying = false;
            UpdatePlayPauseButton();
        }

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mediaElement != null)
            {
                mediaElement.Volume = volumeSlider.Value;
            }
        }

        private void timelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isDraggingSlider && mediaElement.NaturalDuration.HasTimeSpan)
            {
                mediaElement.Position = TimeSpan.FromSeconds(timelineSlider.Value);
            }
        }

        private void timelineSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSlider = true;
        }

        private void timelineSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSlider = false;
            if (mediaElement.NaturalDuration.HasTimeSpan)
            {
                mediaElement.Position = TimeSpan.FromSeconds(timelineSlider.Value);
            }
        }

        private void mediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (mediaElement.NaturalDuration.HasTimeSpan)
            {
                timelineSlider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
                totalTimeText.Text = FormatTime(mediaElement.NaturalDuration.TimeSpan);
            }
            else
            {
                totalTimeText.Text = "--:--";
            }

            if (_isPlaying)
            {
                mediaElement.Play();
            }

            _timer.Start();
        }

        private void mediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            mediaElement.Stop();
            _isPlaying = false;
            UpdatePlayPauseButton();
            timelineSlider.Value = 0;
            _timer.Stop();
        }

        private void mediaElement_MediaFailed(object? sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show($"Playback failed: {e.ErrorException.Message}", "Media Error", MessageBoxButton.OK, MessageBoxImage.Error);
            _isPlaying = false;
            UpdatePlayPauseButton();
        }

        private string FormatTime(TimeSpan time)
        {
            if (time.TotalHours >= 1)
                return time.ToString(@"hh\:mm\:ss");
            return time.ToString(@"mm\:ss");
        }
    }
}
