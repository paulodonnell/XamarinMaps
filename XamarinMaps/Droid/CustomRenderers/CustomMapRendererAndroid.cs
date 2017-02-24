using System;
using Xamarin.Forms;
using XamarinMaps;
using XamarinMaps.Droid;
using Xamarin.Forms.Maps.Android;
using Android.Gms.Maps;
using Android.Locations;
using Android.App;
using Android.Gms.Maps.Model;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TK.CustomMap.Api.Google;
using System.Linq;
using Android.Graphics;
using Xamarin.Forms.Maps;

[assembly: ExportRenderer (typeof(CustomMap), typeof(CustomMapRendererAndroid))]
namespace XamarinMaps.Droid
{
    public class CustomMapRendererAndroid : MapRenderer, IOnMapReadyCallback
    {
        LocationManager locationManager;

        GoogleMap googleMap;
        Android.Locations.Geocoder geocoder;

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
            geocoder = new Android.Locations.Geocoder(FormsActivity);
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
                oldCustomMap.CalculateRouteNativeHandler -= CalculateRoute;
                //oldCustomMap.ClearRouteNativeHandler -= ClearRoute;
                oldCustomMap.CenterOnUsersLocationNativeHandler -= CenterOnUsersLocation;
                //oldCustomMap.SearchLocalNativeHandler -= SearchLocal;
            }

            if (e.NewElement != null)
            {
                CustomMap newCustomMap = e.NewElement as CustomMap;
                //newCustomMap.CalculateRouteFromUserLocationNativeHandler += CalculateRouteFromUserLocation;
                newCustomMap.CalculateRouteNativeHandler += CalculateRoute;
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

        async void CalculateRoute(object sender, EventArgs e)
        {
            try
            {
                IList<Address> sourceAddressList = await geocoder.GetFromLocationAsync(CustomMapView.RouteSource.Latitude, CustomMapView.RouteSource.Longitude, 1);

                if(sourceAddressList.Count > 0)
                {
                    IList<Address> destAddressList = await geocoder.GetFromLocationAsync(CustomMapView.RouteDestination.Latitude, CustomMapView.RouteDestination.Longitude, 1);

                    if(destAddressList.Count > 0)
                    {
                        LatLng sourceLatLng = new LatLng(sourceAddressList[0].Latitude, sourceAddressList[0].Longitude);
                        LatLng destLatLng = new LatLng(destAddressList[0].Latitude, destAddressList[0].Longitude);

                        string strGoogleDirectionUrl = BuildGoogleDirectionUrl(sourceLatLng, destLatLng);

                        string strJSONDirectionResponse = await HttpRequest(strGoogleDirectionUrl);

                        if ( strJSONDirectionResponse != "error" )
                        {
                            //mark source and destination point
                            MarkOnMap("Source", sourceLatLng);
                            MarkOnMap("Destination", destLatLng);
                        }

                        GmsDirectionResult routeData = JsonConvert.DeserializeObject<GmsDirectionResult>(strJSONDirectionResponse);

                        if (routeData != null && routeData.Routes != null)
                        {
                            if (routeData.Status == GmsDirectionResultStatus.Ok)
                            {
                                GmsRouteResult routeResult;
                                TimeSpan timespan;

                                for (int i = routeData.Routes.Count() - 1; i >= 0 ; i--)
                                {
                                    routeResult = routeData.Routes.ElementAt(i);

                                    timespan = routeResult.Duration();
                                    System.Diagnostics.Debug.WriteLine(timespan.ToString(@"hh\:mm\:ss\:fff"));

                                    if (routeResult != null && routeResult.Polyline.Positions != null && routeResult.Polyline.Positions.Count() > 0)
                                    {
                                        PolylineOptions polylineOptions = new PolylineOptions();

                                        if(i == 0)
                                        {
                                            polylineOptions.InvokeColor(Android.Graphics.Color.Blue.ToArgb());                                            
                                        }
                                        else
                                        {
                                            polylineOptions.InvokeColor(Android.Graphics.Color.Gray.ToArgb());
                                        }

                                        foreach (Position position in routeResult.Polyline.Positions)
                                        {
                                            polylineOptions.Add(new LatLng(position.Latitude, position.Longitude));
                                        }

                                        googleMap.AddPolyline(polylineOptions);

                                        //this.SetRouteData(route, r);

                                        //var routeOptions = new PolylineOptions();

                                        //if (route.Color != Color.Default)
                                        //{
                                        //    routeOptions.InvokeColor(route.Color.ToAndroid().ToArgb());
                                        //}
                                        //if (route.LineWidth > 0)
                                        //{
                                        //    routeOptions.InvokeWidth(route.LineWidth);
                                        //}
                                        //routeOptions.Add(r.Polyline.Positions.Select(i => i.ToLatLng()).ToArray());

                                        //this._routes.Add(route, this._googleMap.AddPolyline(routeOptions));

                                        //this.MapFunctions.RaiseRouteCalculationFinished(route);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                return;
            }
        }

        async Task<string> HttpRequest(string strUri)
        {
            WebClient webclient = new WebClient();
            string strResultData;

            try
            {
                strResultData = await webclient.DownloadStringTaskAsync(new Uri(strUri));
                Console.WriteLine(strResultData);
            }
            catch
            {
                strResultData = "error";
            }
            finally
            {
                if (webclient != null)
                {
                    webclient.Dispose();
                    webclient = null;
                }
            }

            return strResultData;
        }

        void MarkOnMap(string title, LatLng pos, int resourceId = -1)
        {
            var marker = new MarkerOptions();
            marker.SetTitle(title);
            marker.SetPosition(pos); //Resource.Drawable.BlueDot

            if(resourceId != -1)
            {
                marker.SetIcon(BitmapDescriptorFactory.FromResource(resourceId));                
            }

            googleMap.AddMarker(marker);
        }

        string BuildGoogleDirectionUrl(LatLng sourceLatLng, LatLng destLatLng)
        {
            return string.Format("https://maps.googleapis.com/maps/api/directions/json?origin={0},{1}&destination={2},{3}&alternatives=true",
                         sourceLatLng.Latitude, sourceLatLng.Longitude, destLatLng.Latitude, destLatLng.Longitude);
            
            //string apiKey = "AIzaSyAp2OFpyy6ktErohUcfi8lCmddZsIxbjWI";

            //return string.Format("https://maps.googleapis.com/maps/api/directions/json?origin={0},{1}&destination={2},{3}&key={4}", 
            //             sourceLatLng.Latitude, sourceLatLng.Longitude, destLatLng.Latitude, destLatLng.Longitude, apiKey);
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
