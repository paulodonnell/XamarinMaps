using System;
using Xamarin.Forms;
using XamarinMaps;
using XamarinMaps.Droid;
using Xamarin.Forms.Maps.Android;
using Android.Gms.Maps;
using Android.Locations;
using Android.App;
using Android.Gms.Maps.Model;

[assembly: ExportRenderer (typeof(CustomMap), typeof(CustomMapRendererAndroid))]
namespace XamarinMaps.Droid
{
    public class CustomMapRendererAndroid : MapRenderer, IOnMapReadyCallback
    {
        LocationManager locationManager;

        GoogleMap googleMap;

        MapView NativeMapView
        {
            get
            {
                return this.Control;
            }
        }

        CustomMap CustomMapView
        {
            get
            {
                return this.Element as CustomMap;
            }
        }

        Activity FormsActivity
        {
            get
            {
                return Xamarin.Forms.Forms.Context as Activity;
            }
        }

        public CustomMapRendererAndroid()
        {
        }

        protected override void OnElementChanged(Xamarin.Forms.Platform.Android.ElementChangedEventArgs<Xamarin.Forms.Maps.Map> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null)
            {
                if(googleMap != null)
                {
                    //remove google map handlers
                }

                CustomMap oldCustomMap = e.OldElement as CustomMap;
                //oldCustomMap.CalculateRouteFromUserLocationNativeHandler -= CalculateRouteFromUserLocation;
                //oldCustomMap.CalculateRouteNativeHandler -= CalculateRoute;
                //oldCustomMap.ClearRouteNativeHandler -= ClearRoute;
                oldCustomMap.CenterOnUsersLocationNativeHandler -= CenterOnUsersLocation;
                //oldCustomMap.SearchLocalNativeHandler -= SearchLocal;
            }

            if (e.NewElement != null)
            {
                CustomMap newCustomMap = e.NewElement as CustomMap;
                //newCustomMap.CalculateRouteFromUserLocationNativeHandler += CalculateRouteFromUserLocation;
                //newCustomMap.CalculateRouteNativeHandler += CalculateRoute;
                //newCustomMap.ClearRouteNativeHandler += ClearRoute;
                newCustomMap.CenterOnUsersLocationNativeHandler += CenterOnUsersLocation;
                //newCustomMap.SearchLocalNativeHandler += SearchLocal;

                //UpdateShowTraffic();

                NativeMapView.GetMapAsync(this);
            }
        }

        public virtual void OnMapReady(GoogleMap googleMap)
        {
            this.googleMap = googleMap;

            googleMap.MyLocationButtonClick += (object sender, GoogleMap.MyLocationButtonClickEventArgs e) => 
            {
                CenterOnUsersLocation(null, null);
            };
        }

        void CenterOnUsersLocation(object sender, EventArgs e)
        {
            if (googleMap == null)
                return;
            
            locationManager = (LocationManager)FormsActivity.GetSystemService(Activity.LocationService);

            Criteria criteria = new Criteria();
            criteria.Accuracy = Accuracy.Fine;

            Location location = locationManager.GetLastKnownLocation(locationManager.GetBestProvider(criteria, false));

            if (location != null)
            {
                googleMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(location.Latitude, location.Longitude), 15));

                //CameraPosition cameraPosition = new CameraPosition.Builder()
                //    .Target(new LatLng(location.Latitude, location.Longitude))      // Sets the center of the map to location user
                //    .Zoom(17)                   // Sets the zoom
                //    .Bearing(90)                // Sets the orientation of the camera to east
                //    .Tilt(40)                   // Sets the tilt of the camera to 30 degrees
                //    .Build();                   // Creates a CameraPosition from the builder

                //googleMap.AnimateCamera(CameraUpdateFactory.NewCameraPosition(cameraPosition));
            }
        }

   }
}
