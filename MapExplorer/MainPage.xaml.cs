/*
 * Copyright © 2012-2013 Nokia Corporation. All rights reserved.
 * Nokia and Nokia Connecting People are registered trademarks of Nokia Corporation. 
 * Other product and company names mentioned herein may be trademarks
 * or trade names of their respective owners. 
 * See LICENSE.TXT for license information.
 */

using System;
using System.Collections.Generic;
using System.Device.Location;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Windows.Devices.Geolocation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Services;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using MapExplorer.Resources;
using Microsoft.Phone.Storage;
using System.Threading.Tasks;
using System.IO;
using MapExplorer.Services;
using System.Windows.Threading;
using MapExplorer.Model;

namespace MapExplorer
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            InitializeCurrentTrack();

            Settings = IsolatedStorageSettings.ApplicationSettings;
        }

        private void InitializeCurrentTrack()
        {
            // create a line which illustrates the run
            _line = new MapPolyline();
            _line.StrokeColor = Colors.Green;
            _line.StrokeThickness = 3;
            MyMap.MapElements.Add(_line);

            App.Watcher.PositionChanged += Watcher_PositionChanged;

            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;

            App.Watcher.Start();
            _timer.Start();

            _startTime = System.Environment.TickCount;

            PhoneApplicationService.Current.ApplicationIdleDetectionMode = IdleDetectionMode.Disabled;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            TimeSpan runTime = TimeSpan.FromMilliseconds(System.Environment.TickCount - _startTime);
            //timeLabel.Text = runTime.ToString(@"hh\:mm\:ss");
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            App.Watcher.PositionChanged -= Watcher_PositionChanged;
            App.Watcher = null;
        }

        private void Watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            var coord = new GeoCoordinate(e.Position.Location.Latitude, e.Position.Location.Longitude);

            if (_line.Path.Any())
            {
                var previousPoint = _line.Path.Last();
                var distance = coord.GetDistanceTo(previousPoint);
                //var millisPerKilometer = (1000.0 / distance) * (System.Environment.TickCount - _previousPositionChangeTick);
                //_kilometres += distance / 1000.0;

                //paceLabel.Text = TimeSpan.FromMilliseconds(millisPerKilometer).ToString(@"mm\:ss");
                //distanceLabel.Text = string.Format("{0:f2} km", _kilometres);
                //caloriesLabel.Text = string.Format("{0:f0}", _kilometres * 65);

                //PositionHandler handler = new PositionHandler();
                //var heading = handler.CalculateBearing(new Position(previousPoint), new Position(coord));
                //Map.SetView(coord, Map.ZoomLevel, heading, MapAnimationKind.Parabolic);

                //ShellTile.ActiveTiles.First().Update(new IconicTileData()
                //{
                //    Title = "WP8Runner",
                //    WideContent1 = string.Format("{0:f2} km", _kilometres),
                //    WideContent2 = string.Format("{0:f0} calories", _kilometres * 65),
                //});
            }
            else
            {
                MyMap.Center = coord;
                MyMap.ZoomLevel = 15;
            }

            if (e.Position.Location.HorizontalAccuracy < 100)
            {
                _line.Path.Add(coord);
            }


            if (!App.AppRunningInBackground)
            {
                DrawCurrentPosition(coord);
            }
        }

        private void DrawCurrentPosition(GeoCoordinate coord)
        {
            MyMap.Layers.Clear();
            MapLayer mapLayer = new MapLayer();

            DrawMapMarker(coord, Colors.Green, mapLayer);
            MyMap.Layers.Add(mapLayer);
        }

        /// <summary>
        /// Helper method to draw a single marker on top of the map.
        /// </summary>
        /// <param name="coordinate">GeoCoordinate of the marker</param>
        /// <param name="color">Color of the marker</param>
        /// <param name="mapLayer">Map layer to add the marker</param>
        private void DrawMapMarker(GeoCoordinate coordinate, Color color, MapLayer mapLayer)
        {
            // Create a map marker
            var ellipse = new Ellipse();
            ellipse.Height = 30;
            ellipse.Width = 30;
            ellipse.Fill = new SolidColorBrush(color);

            // Create a MapOverlay and add marker.
            MapOverlay overlay = new MapOverlay();
            overlay.Content = ellipse;
            overlay.GeoCoordinate = new GeoCoordinate(coordinate.Latitude, coordinate.Longitude);
            overlay.PositionOrigin = new Point(0.5, 0.5);
            mapLayer.Add(overlay);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (_isNewInstance)
            {
                _isNewInstance = false;

                LoadSettings();
                if (_isLocationAllowed)
                {
                    LocationPanel.Visibility = Visibility.Collapsed;
                    BuildApplicationBar();
                    LoadTracks();
                }
            }
        }

        private List<Track> _tracks = new List<Track>();

        private async void LoadTracks()
        {
            var parser = new GpxParser();

            ExternalStorageDevice sdCard = (await ExternalStorage.GetExternalStorageDevicesAsync()).FirstOrDefault();
            if (sdCard != null)
            {
                var folder = await sdCard.GetFolderAsync("D:\\Tracks\\");
                if (folder != null)
                {
                    var files = await folder.GetFilesAsync();
                    foreach (ExternalStorageFile file in files)
                    {
                        var track = new Track { Name = file.Name };
                        string winRtPath = "D:\\" + file.Path;
                        var segments = await Task.Run(() => parser.GetCoordinates(winRtPath));
                        foreach (var segment in segments)
                        {
                            var line = new MapPolyline
                            {
                                StrokeColor = Colors.Red,
                                StrokeThickness = 3,
                                Path = segment,
                            };
                            MyMap.MapElements.Add(line);
                            track.Segments.Add(line);
                        }
                        _tracks.Add(track);
                    }
                }
            }

            if (_line != null)
            {
                MyMap.MapElements.Remove(_line);
                MyMap.MapElements.Add(_line);
            }

            AppBarHighlightMenuItem.IsEnabled = true;
        }

        /// <summary>
        /// Event handler for location usage permission at startup.
        /// </summary>
        private void LocationUsage_Click(object sender, EventArgs e)
        {
            LocationPanel.Visibility = Visibility.Collapsed;
            BuildApplicationBar();
            if (sender == AllowButton)
            {
                _isLocationAllowed = true;
                SaveSettings();

                LoadTracks();
            }
        }

        /// <summary>
        /// We must satisfy Maps API's Terms and Conditions by specifying
        /// the required Application ID and Authentication Token.
        /// See http://msdn.microsoft.com/en-US/library/windowsphone/develop/jj207033(v=vs.105).aspx#BKMK_appidandtoken
        /// </summary>
        private void MyMap_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
#warning Please obtain a valid application ID and authentication token.
#else
#error You must specify a valid application ID and authentication token.
#endif
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = "__ApplicationID__";
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = "__AuthenticationToken__";
        }




        /// <summary>
        /// Event handler for clicking about menu item.
        /// </summary>
        private void About_Click(object sender, EventArgs e)
        {
            // Clear map layers to avoid map markers briefly shown on top of about page 
            MyMap.Layers.Clear();
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        void centerButton_Click(object sender, EventArgs e)
        {
            if (_line != null && _line.Path.Any())
            {
                MyMap.Center = _line.Path.Last();
            }
        }


        void startButton_Click(object sender, EventArgs e)
        {
            var button = sender as ApplicationBarIconButton;

            if (_timer.IsEnabled)
            {
                button.IconUri = new Uri("/Assets/appbar.start.png", UriKind.Relative);
                button.Text = "Resume";
                App.Watcher.Stop();
                _timer.Stop();
            }
            else
            {
                button.IconUri = new Uri("/Assets/appbar.pause.png", UriKind.Relative);
                button.Text = "Pause";
                App.Watcher.Start();
                _timer.Start();
            }
        }


        /// <summary>
        /// Event handler for map zoom level value change.
        /// Drawing accuracy radius has dependency on map zoom level.
        /// </summary>
        private void ZoomLevelChanged(object sender, EventArgs e)
        {
            RecalculateScale();
        }

        private void RecalculateScale()
        {
            double metersPerPixels = (Math.Cos(MyMap.Center.Latitude * Math.PI / 180) * 2 * Math.PI * 6378137) / (256 * Math.Pow(2, MyMap.ZoomLevel));
            double kmLength = (double)1000 / metersPerPixels;

            var scaleLength = 200 * metersPerPixels;

            var unit = "m";
            var format = "0";
            if (scaleLength > 1000)
            {
                unit = "km";
                scaleLength = scaleLength / 1000;

                if (scaleLength < 10)
                {
                    format = "0.#";
                }
            }

            ScaleRectangle.Width = 200;
            ScaleText.Text = scaleLength.ToString(format) + unit;
        }

        /// <summary>
        /// Helper method to build a localized ApplicationBar
        /// </summary>
        private void BuildApplicationBar()
        {
            // Set the page's ApplicationBar to a new instance of ApplicationBar.    
            ApplicationBar = new ApplicationBar();

            ApplicationBar.Mode = ApplicationBarMode.Default;
            ApplicationBar.IsVisible = true;
            ApplicationBar.Opacity = 1.0;
            ApplicationBar.IsMenuEnabled = true;

            // Create new buttons with the localized strings from AppResources.
            ApplicationBarIconButton centerButton = new ApplicationBarIconButton(new Uri("/Assets/appbar.locate.me.png", UriKind.Relative));
            centerButton.Text = AppResources.LocateMeMenuButtonText;
            centerButton.Click += centerButton_Click;
            ApplicationBar.Buttons.Add(centerButton);

            ApplicationBarIconButton startButton = new ApplicationBarIconButton(new Uri("/Assets/appbar.pause.png", UriKind.Relative));
            startButton.Text = "Pause";
            startButton.Click += startButton_Click;
            ApplicationBar.Buttons.Add(startButton);

            ApplicationBarIconButton saveButton = new ApplicationBarIconButton(new Uri("/Assets/appbar.save.png", UriKind.Relative));
            saveButton.Text = "Save";
            saveButton.Click += saveButton_Click;
            ApplicationBar.Buttons.Add(saveButton);


            // Create new menu items with the localized strings from AppResources.
            AppBarAboutMenuItem = new ApplicationBarMenuItem(AppResources.AboutMenuItemText);
            AppBarAboutMenuItem.Click += new EventHandler(About_Click);
            ApplicationBar.MenuItems.Add(AppBarAboutMenuItem);

            AppBarHighlightMenuItem = new ApplicationBarMenuItem("Highlight");
            AppBarHighlightMenuItem.Click += highlightMenuItem_Click;
            AppBarHighlightMenuItem.IsEnabled = false;
            ApplicationBar.MenuItems.Add(AppBarHighlightMenuItem);
        }

        void highlightMenuItem_Click(object sender, EventArgs e)
        {
            tracksSelector.ItemsSource = _tracks;
            tracksSelector.Visibility = System.Windows.Visibility.Visible;
            tracksSelector.SelectionChanged += tracksSelector_SelectionChanged;
        }

        private Track _highlightedTrack;

        void tracksSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tracksSelector.Visibility = System.Windows.Visibility.Collapsed;
            var selectedTrack = tracksSelector.SelectedItem as Track;
            if (selectedTrack != null)
            {
                if (_highlightedTrack != null)
                {
                    foreach (var segment in _highlightedTrack.Segments)
                    {
                        segment.StrokeColor = Colors.Red;
                    }
                }
                _highlightedTrack = selectedTrack;
                foreach (var segment in selectedTrack.Segments)
                {
                    segment.StrokeColor = Colors.Blue;
                    MyMap.MapElements.Remove(segment);
                    MyMap.MapElements.Add(segment);
                }

                if (_line != null)
                {
                    MyMap.MapElements.Remove(_line);
                    MyMap.MapElements.Add(_line);
                }
            }
        }

        void saveButton_Click(object sender, EventArgs e)
        {
            var creator = new GpxCreator();
            var task = creator.CreateGpxFile(_line.Path);

            task.ContinueWith(async (c) =>
            {
                var successful = await c;
                var message = successful ? "File created successfully" : "There was an error creating the file.";
                Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(message);
                });
            });
        }




        /// <summary>
        /// Helper method to show progress indicator in system tray
        /// </summary>
        /// <param name="msg">Text shown in progress indicator</param>
        private void ShowProgressIndicator(String msg)
        {
            if (ProgressIndicator == null)
            {
                ProgressIndicator = new ProgressIndicator();
                ProgressIndicator.IsIndeterminate = true;
            }
            ProgressIndicator.Text = msg;
            ProgressIndicator.IsVisible = true;
            SystemTray.SetProgressIndicator(this, ProgressIndicator);
        }

        /// <summary>
        /// Helper method to hide progress indicator in system tray
        /// </summary>
        private void HideProgressIndicator()
        {
            ProgressIndicator.IsVisible = false;
            SystemTray.SetProgressIndicator(this, ProgressIndicator);
        }

        /// <summary>
        /// Helper method to load application settings
        /// </summary>
        public void LoadSettings()
        {
            if (Settings.Contains("isLocationAllowed"))
            {
                _isLocationAllowed = (bool)Settings["isLocationAllowed"];
            }
        }

        /// <summary>
        /// Helper method to save application settings
        /// </summary>
        public void SaveSettings()
        {
            if (Settings.Contains("isLocationAllowed"))
            {
                if ((bool)Settings["isLocationAllowed"] != _isLocationAllowed)
                {
                    // Store the new value
                    Settings["isLocationAllowed"] = _isLocationAllowed;
                }
            }
            else
            {
                Settings.Add("isLocationAllowed", _isLocationAllowed);
            }
        }

        // Application bar menu items
        private ApplicationBarMenuItem AppBarAboutMenuItem = null;
        private ApplicationBarMenuItem AppBarHighlightMenuItem = null;

        // Progress indicator shown in system tray
        private ProgressIndicator ProgressIndicator = null;



        /// <summary>
        /// True when this object instance has been just created, otherwise false
        /// </summary>
        private bool _isNewInstance = true;

        /// <summary>
        /// True when access to user location is allowed, otherwise false
        /// </summary>
        private bool _isLocationAllowed = false;




        /// <summary>
        /// Used for saving location usage permission
        /// </summary>
        private IsolatedStorageSettings Settings;



        private MapPolyline _line;
        private DispatcherTimer _timer = new DispatcherTimer();
        private long _startTime;
    }
}