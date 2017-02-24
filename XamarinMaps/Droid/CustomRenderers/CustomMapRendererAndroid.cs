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
        LocationManager locMgr;
        Criteria locCriteria;

        GoogleMap googleMap;
        Android.Locations.Geocoder geocoder;

        List<Polyline> polylineList;
        List<Marker> markerList;

        GmsRouteResult currRouteResult;

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

            locMgr = (LocationManager)FormsActivity.GetSystemService(Activity.LocationService);

            locCriteria = new Criteria();
            locCriteria.Accuracy = Accuracy.Fine;

            polylineList = new List<Polyline>();
            markerList = new List<Marker>();
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
                oldCustomMap.CalculateRouteFromUserLocationNativeHandler -= CalculateRouteFromUserLocation;
                oldCustomMap.CalculateRouteNativeHandler -= CalculateRoute;
                oldCustomMap.ClearRouteNativeHandler -= ClearRoute;
                oldCustomMap.CenterOnUsersLocationNativeHandler -= CenterOnUsersLocation;
                //oldCustomMap.SearchLocalNativeHandler -= SearchLocal;
            }

            if (e.NewElement != null)
            {
                CustomMap newCustomMap = e.NewElement as CustomMap;
                newCustomMap.CalculateRouteFromUserLocationNativeHandler += CalculateRouteFromUserLocation;
                newCustomMap.CalculateRouteNativeHandler += CalculateRoute;
                newCustomMap.ClearRouteNativeHandler += ClearRoute;
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

        void CalculateRoute(object sender, EventArgs e)
        {
            Task.Run(CalculateRouteAsync);
        }

        async Task CalculateRouteAsync()
        {
            try
            {
                ClearRoute();

                LatLng sourceLatLng = await GetLatLngForPositionAsync(CustomMapView.RouteSource);
                LatLng destLatLng = await GetLatLngForPositionAsync(CustomMapView.RouteDestination);

                if(sourceLatLng != null && destLatLng != null)
                {
                    CalculateRouteDetailsAysnc(sourceLatLng, destLatLng, true, true);                       
                }               
            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                return;
            }
        }

        void CalculateRouteFromUserLocation(object sender, EventArgs e)
        {  
            Task.Run(CalculateRouteFromUserLocationAysnc);
        }

        async Task CalculateRouteFromUserLocationAysnc()
        {
            try
            {
                ClearRoute();

                LatLng sourceLatLng = GetUserLatLng();
                LatLng destLatLng = await GetLatLngForPositionAsync(CustomMapView.RouteDestination);

                if (sourceLatLng != null && destLatLng != null)
                {
                    CalculateRouteDetailsAysnc(sourceLatLng, destLatLng, false, true);
                }
            }
            catch
            {
                return;
            }
        }

        async void CalculateRouteDetailsAysnc(LatLng sourceLatLng, LatLng destLatLng, bool markSource, bool markDest)
        {
            string strGoogleDirectionUrl = BuildGoogleDirectionUrl(sourceLatLng, destLatLng);

            string strJSONDirectionResponse = await HttpRequest(strGoogleDirectionUrl);

            if (strJSONDirectionResponse != "error")
            {
                if(markSource)
                    MarkOnMap("Source", sourceLatLng);

                if(markDest)
                    MarkOnMap("Destination", destLatLng);
            }

            GmsDirectionResult routeData = JsonConvert.DeserializeObject<GmsDirectionResult>(strJSONDirectionResponse);

            if (routeData != null && routeData.Routes != null)
            {
                if (routeData.Status == GmsDirectionResultStatus.Ok)
                {
                    GmsRouteResult routeResult;
                    TimeSpan timespan;

                    for (int i = routeData.Routes.Count() - 1; i >= 0; i--)
                    {
                        routeResult = routeData.Routes.ElementAt(i);

                        timespan = routeResult.Duration();
                        System.Diagnostics.Debug.WriteLine(timespan.ToString(@"hh\:mm\:ss\:fff"));

                        if (routeResult != null && routeResult.Polyline.Positions != null && routeResult.Polyline.Positions.Count() > 0)
                        {
                            PolylineOptions polylineOptions = new PolylineOptions();

                            if (i == 0)
                            {
                                currRouteResult = routeResult;
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

                            Device.BeginInvokeOnMainThread(() => 
                            {
                                polylineList.Add(googleMap.AddPolyline(polylineOptions));                                

                                if (routeResult == currRouteResult)
                                {
                                    LatLng sw = new LatLng(routeResult.Bounds.SouthWest.Latitude, routeResult.Bounds.SouthWest.Longitude);
                                    LatLng ne = new LatLng(routeResult.Bounds.NorthEast.Latitude, routeResult.Bounds.NorthEast.Longitude);

                                    googleMap.AnimateCamera(CameraUpdateFactory.NewLatLngBounds(new LatLngBounds(sw, ne), 120));
                                }
                            });
                        }
                    }
                }
            }
        }

        async Task<LatLng> GetLatLngForPositionAsync(Position pos)
        {
            IList<Address> addressList = await geocoder.GetFromLocationAsync(pos.Latitude, pos.Longitude, 1);

            if (addressList.Count > 0)
            {
                return new LatLng(addressList[0].Latitude, addressList[0].Longitude);
            }

            return null;
        }

        async Task<string> HttpRequest(string strUri)
        {
            WebClient webclient = new WebClient();
            string strResultData;

            try
            {
                strResultData = await webclient.DownloadStringTaskAsync(new Uri(strUri));
                //Console.WriteLine(strResultData);
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

            Device.BeginInvokeOnMainThread(() =>                                         
            {
                markerList.Add(googleMap.AddMarker(marker));                
            });
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

            LatLng loc = GetUserLatLng();

            if (loc != null)
            {
                googleMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(loc, 15));

                //CameraPosition cameraPosition = new CameraPosition.Builder()
                //    .Target(new LatLng(location.Latitude, location.Longitude))      // Sets the center of the map to location user
                //    .Zoom(17)                   // Sets the zoom
                //    .Bearing(90)                // Sets the orientation of the camera to east
                //    .Tilt(40)                   // Sets the tilt of the camera to 30 degrees
                //    .Build();                   // Creates a CameraPosition from the builder

                //googleMap.AnimateCamera(CameraUpdateFactory.NewCameraPosition(cameraPosition));
            }
        }

        LatLng GetUserLatLng()
        {
            Location location = locMgr.GetLastKnownLocation(locMgr.GetBestProvider(locCriteria, false));

            if(location != null)
            {
                return new LatLng(location.Latitude, location.Longitude);
            }

            return null;
        }

        void ClearRoute(object sender = null, EventArgs e = null)
        {
            currRouteResult = null;

            Device.BeginInvokeOnMainThread(() =>
            {
                ClearMarkers();
                ClearPolylines();                
            });
        }

        void ClearMarkers()
        {
            foreach(Marker m in markerList)
            {
                m.Remove();
            }

            markerList.Clear();
        }

        void ClearPolylines()
        {
            foreach(Polyline pl in polylineList)
            {
                pl.Remove();
            }

            polylineList.Clear();
        }

   }
}
