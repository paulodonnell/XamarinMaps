﻿using System;
using Xamarin.Forms;
using Xamarin.Forms.Maps;

namespace XamarinMaps
{
    public partial class XamarinMapsPage : ContentPage
    {
        public XamarinMapsPage()
        {
            InitializeComponent();

            //Map map = new Map(
            //MapSpan.FromCenterAndRadius(
            //        new Position(37, -122), Distance.FromMiles(0.3)))
            //{
            //    IsShowingUser = true,
            //    HeightRequest = 100,
            //    WidthRequest = 960,
            //    VerticalOptions = LayoutOptions.FillAndExpand
            //};

            //stackLayout.Children.Add(map);

            map.IsShowingUser = true;

            InitSearch();
            InitBtns();
        }

        void InitSearch()
        {
            searchStack.Padding = new Thickness(10, 0);
            searchStack.Spacing = 20;

            searchEntry.Placeholder = "Enter text...";
            searchEntry.HorizontalOptions = LayoutOptions.FillAndExpand;
            searchEntry.Completed += StartSearch;

            searchBtn.Text = "Search";
            searchBtn.Clicked += StartSearch;
        }

        void StartSearch(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(searchEntry.Text))
            {
                map.SearchLocal(searchEntry.Text);
            }
        }

        void InitBtns()
        {
            btnStack.HorizontalOptions = LayoutOptions.Center;
            btnStack.BackgroundColor = Color.Transparent;
            btnStack.Spacing = 20;

            navigateBtn.Text = "Navigate";
            navigateBtn.Clicked += (object sender, EventArgs e) =>
            {
                InitNavigation();
            };

            showRouteBtn.Text = "Show Route";
            showRouteBtn.Clicked += (object sender, EventArgs e) => 
            {
                InitMapRoute();
            };

            clearRouteBtn.Text = "Clear";
            clearRouteBtn.Clicked += (object sender, EventArgs e) => 
            {
                searchEntry.Text = null;
                map.ClearRoute();
            };

            toggleLocation.Text = "Loc.";
            toggleLocation.Clicked += (object sender, EventArgs e) =>
            {
                if(Device.OS == TargetPlatform.iOS && CustomMap.LocationAuthStatus == CustomMap.LocAuthStatus.NotAllowed)
                {
                    DisplayAlert("Location Services Not Enabled", "Enable location services in Settings", "Ok");
                }
                else
                {
                    map.CenterOnUsersLocation();                                        
                }
            };
        }

        void InitNavigation()
        {
            Pin sligoPin = new Pin();
            sligoPin.Label = "Sligo";
            sligoPin.Position = new Position(54.2766, -8.4761);
            sligoPin.Type = PinType.SavedPin;

            map.CalculateRouteFromUserLocation(sligoPin.Position);
        }

        void InitMapRoute()
        {
            Pin dublinPin = new Pin();
            dublinPin.Label = "Dublin";
            dublinPin.Position = new Position(53.3498, -6.2603);

            Pin roebuckPin = new Pin();
            roebuckPin.Label = "Roebuck";
            roebuckPin.Position = new Position(53.3, -6.22);
            roebuckPin.Type = PinType.SavedPin;

            map.CalculateRoute(dublinPin.Position, roebuckPin.Position);
        }
   }
}
