using System;
using Xamarin.Forms;
using Xamarin.Forms.Maps.iOS;
using XamarinMaps;
using XamarinMaps.iOS;
using CoreLocation;
using MapKit;
using UIKit;
using System.Collections.Generic;

[assembly: ExportRenderer (typeof(CustomMap), typeof(CustomMapRendererIos))]
namespace XamarinMaps.iOS
{
    public class CustomMapRendererIos : MapRenderer
    {
        CLLocationManager locationManager;
        bool centerOnUserRequested;

        List<IMKAnnotation> annotationsList;
        List<IMKOverlay> overlaysList;

        MKPolylineRenderer polylineRenderer;

        MKMapView NativeMapView
        {
            get
            {
                return this.Control as MKMapView;
            }
        }

        CustomMap CustomMapView
        {
            get
            {
                return this.Element as CustomMap;
            }
        }

        public CustomMapRendererIos()
        {
            annotationsList = new List<IMKAnnotation>();
            overlaysList = new List<IMKOverlay>();

            locationManager = new CLLocationManager();
            locationManager.RequestWhenInUseAuthorization();
        }

        protected override void OnElementChanged(Xamarin.Forms.Platform.iOS.ElementChangedEventArgs<View> e)
        {
            base.OnElementChanged(e);

            if(e.OldElement != null)
            {
                locationManager.AuthorizationChanged -= OnLocationAuthChanged;

                NativeMapView.OverlayRenderer = null;
                NativeMapView.DidUpdateUserLocation -= OnUserLocationUpdated;

                CustomMap oldCustomMap = e.OldElement as CustomMap;
                oldCustomMap.CalculateRouteNativeHandler -= CalculateRoute;
                oldCustomMap.ClearRouteNativeHandler -= ClearRoute;
                oldCustomMap.CenterOnUsersLocationNativeHandler -= CenterOnUsersLocation;
            }

            if(e.NewElement != null)
            {
                locationManager.AuthorizationChanged += OnLocationAuthChanged;

                NativeMapView.OverlayRenderer = GetOverlayRenderer;
                NativeMapView.DidUpdateUserLocation += OnUserLocationUpdated;

                CustomMap newCustomMap = e.NewElement as CustomMap;
                newCustomMap.CalculateRouteNativeHandler += CalculateRoute;
                newCustomMap.ClearRouteNativeHandler += ClearRoute;
                newCustomMap.CenterOnUsersLocationNativeHandler += CenterOnUsersLocation;

                UpdateShowTraffic();
            }
        }

        protected override void OnElementPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == CustomMap.ShowTrafficProperty.PropertyName)
            {
                UpdateShowTraffic();
            }
        }

        void OnLocationAuthChanged(object sender, CLAuthorizationChangedEventArgs e)
        {
            if(e.Status == CLAuthorizationStatus.Denied)
            {
                CustomMapView.LocationAuthStatus = CustomMap.LocAuthStatus.NotAllowed;
            }
            else if (e.Status == CLAuthorizationStatus.AuthorizedWhenInUse)
            {
                CustomMapView.LocationAuthStatus = CustomMap.LocAuthStatus.AllowedInForeground;
            }
            else if (e.Status == CLAuthorizationStatus.AuthorizedAlways)
            {
                CustomMapView.LocationAuthStatus = CustomMap.LocAuthStatus.AllowedInBackground;
            }
        }


        void OnUserLocationUpdated(object sender, MKUserLocationEventArgs e)
        {
            if(centerOnUserRequested)
            {
                CenterOnUsersLocation(null, null);
                centerOnUserRequested = false;
            }

            CustomMapView.OnUserLocationUpdatedFromNative(new Xamarin.Forms.Maps.Position(e.UserLocation.Location.Coordinate.Longitude, 
                                                                                          e.UserLocation.Location.Coordinate.Latitude));
        }

        void CenterOnUsersLocation(object sender, EventArgs e)
        {
            if (NativeMapView.UserLocation.Location != null)
            {
                
                CLLocationCoordinate2D coords = NativeMapView.UserLocation.Coordinate;
                MKCoordinateSpan span = new MKCoordinateSpan(KilometresToLatitudeDegrees(2), KilometresToLongitudeDegrees(2, coords.Latitude));
                NativeMapView.Region = new MKCoordinateRegion(coords, span);
            }
            else
            {
                centerOnUserRequested = true;
            }
        }

        void UpdateShowTraffic()
        {
            NativeMapView.ShowsTraffic = CustomMapView.ShowTraffic;
        }

        void CalculateRoute(object sender, EventArgs e)
        {
            CustomMap customMap = this.Element as CustomMap;
            MKMapView nativeMapView = this.Control as MKMapView;

            CLLocationCoordinate2D sourceLocation = new CLLocationCoordinate2D(customMap.RouteSource.Latitude, customMap.RouteSource.Longitude);
            CLLocationCoordinate2D destinationLocation = new CLLocationCoordinate2D(customMap.RouteDestination.Latitude, customMap.RouteDestination.Longitude);

            MKPlacemark sourcePlacemark = new MKPlacemark(coordinate: sourceLocation);
            MKPlacemark destinationPlacemark = new MKPlacemark(coordinate: destinationLocation);

            MKMapItem sourceMapItem = new MKMapItem(sourcePlacemark);
            MKMapItem destinationMapItem = new MKMapItem(destinationPlacemark);
                    
            MKPointAnnotation sourceAnnotation = new MKPointAnnotation();
            //sourceAnnotation.Title = "Source";
            sourceAnnotation.Coordinate = sourcePlacemark.Location.Coordinate;

            MKPointAnnotation destinationAnnotation = new MKPointAnnotation();
            //destinationAnnotation.Title = "Destination";
            destinationAnnotation.Coordinate = destinationPlacemark.Location.Coordinate;

            //save annotations so they can be removed later if requested
            annotationsList.Add(sourceAnnotation);
            annotationsList.Add(destinationAnnotation);

            nativeMapView.ShowAnnotations(new IMKAnnotation[]{sourceAnnotation, destinationAnnotation}, true);

            MKDirectionsRequest directionRequest = new MKDirectionsRequest();
            directionRequest.Source = sourceMapItem;
            directionRequest.Destination = destinationMapItem;
            directionRequest.TransportType = MKDirectionsTransportType.Automobile;

            MKDirections directions = new MKDirections(directionRequest);

            directions.CalculateDirections((MKDirectionsResponse response, Foundation.NSError error) =>
            {
                if(error != null)
                {
                    System.Diagnostics.Debug.WriteLine(error.Description);
                }
                else
                {
                    MKRoute route = response.Routes[0];

                    //save overlay so it can be removed later if requested
                    overlaysList.Add(route.Polyline);

                    nativeMapView.AddOverlay(route.Polyline, MKOverlayLevel.AboveRoads);

                    //MKMapRect rect = route.Polyline.BoundingMapRect;
                    //MKMapRect expandedRect = nativeMapView.MapRectThatFits(rect, new UIEdgeInsets(20,20,20,20));

                    //nativeMapView.SetRegion(MKCoordinateRegion.FromMapRect(expandedRect), true);
                }
            });                
        }

        void ClearRoute(object sender, EventArgs e)
        {
            NativeMapView.RemoveAnnotations(annotationsList.ToArray());
            annotationsList.Clear();

            NativeMapView.RemoveOverlays(overlaysList.ToArray());
            overlaysList.Clear();
        }

        MKOverlayRenderer GetOverlayRenderer(MKMapView mapView, IMKOverlay overlay)
        {
            if (polylineRenderer == null)
            {
                polylineRenderer = new MKPolylineRenderer(overlay as MKPolyline);
                polylineRenderer.StrokeColor = UIColor.Blue;
                polylineRenderer.LineWidth = 5;
            }

            return polylineRenderer;
        }

        /// <summary>Converts kilometres to latitude degrees</summary>
        public double KilometresToLatitudeDegrees(double kms)
        {
            double earthRadius = 6371.0; // in kms
            double radiansToDegrees = 180.0 / Math.PI;
            return (kms / earthRadius) * radiansToDegrees;
        }

        /// <summary>Converts kilometres to longitudinal degrees at a specified latitude</summary>
        public double KilometresToLongitudeDegrees(double kms, double atLatitude)
        {
            double earthRadius = 6371.0; // in kms
            double degreesToRadians = Math.PI / 180.0;
            double radiansToDegrees = 180.0 / Math.PI;
            // derive the earth's radius at that point in latitude
            double radiusAtLatitude = earthRadius * Math.Cos(atLatitude * degreesToRadians);
            return (kms / radiusAtLatitude) * radiansToDegrees;
        }
   }
}
