using System;
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

            InitMapRoute();
            InitBtns();
        }

        void InitBtns()
        {
            btnStack.HorizontalOptions = LayoutOptions.Center;
            btnStack.BackgroundColor = Color.Transparent;
            btnStack.Spacing = 20;

            showRouteBtn.Text = "Show Route";
            showRouteBtn.Clicked += (object sender, EventArgs e) => 
            {
                InitMapRoute();
            };

            clearRouteBtn.Text = "Clear Route";
            clearRouteBtn.Clicked += (object sender, EventArgs e) => 
            {
                map.ClearRoute();
            };

            toggleLocation.Text = "My Loc";
            toggleLocation.Clicked += (object sender, EventArgs e) =>
            {
                if(map.LocationAuthStatus == CustomMap.LocAuthStatus.NotAllowed)
                {
                    DisplayAlert("Location Services Not Enabled", "Enable location services in Settings", "Ok");
                }
                else
                {
                    map.CenterOnUsersLocation();                                        
                }
            };
        }

        void InitMapRoute()
        {
            Pin dublinPin = new Pin();
            dublinPin.Label = "Dublin";
            dublinPin.Position = new Position(53.3498, -6.2603);

            Pin sligoPin = new Pin();
            sligoPin.Label = "Sligo";
            sligoPin.Position = new Position(54.2766, -8.4761);
            sligoPin.Type = PinType.SavedPin;

            Pin roebuckPin = new Pin();
            roebuckPin.Label = "Roebuck";
            roebuckPin.Position = new Position(53.3, -6.22);
            roebuckPin.Type = PinType.SavedPin;

            //float zoom = 0.3f; //100;
            //map.MoveToRegion(MapSpan.FromCenterAndRadius(dublinPin.Position, Distance.FromMiles(zoom)));
            //map.Pins.Add(dublinPin);
            //map.Pins.Add(sligoPin);

            map.CalculateRoute(dublinPin.Position, roebuckPin.Position);
        }
   }
}
