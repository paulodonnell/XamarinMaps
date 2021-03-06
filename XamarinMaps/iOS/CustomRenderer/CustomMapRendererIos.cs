﻿using System;
using Xamarin.Forms;
using Xamarin.Forms.Maps.iOS;
using XamarinMaps;
using XamarinMaps.iOS;
using CoreLocation;
using MapKit;
using UIKit;
using System.Collections.Generic;
using CoreGraphics;

[assembly: ExportRenderer (typeof(CustomMap), typeof(CustomMapRendererIos))]
namespace XamarinMaps.iOS
{
    public class CustomMapRendererIos : MapRenderer
    {
        private const string DefaultPinId = "defaultPin";

        private static readonly UIColor AltRouteColor = UIColor.LightGray;
        private static readonly UIColor RouteColor = UIColor.FromRGBA(0, 179, 253, 255);

        CLLocationManager locMgr;
        private CLLocation lastUserLocation;
        bool centerOnUserRequested;

        List<IMKAnnotation> annotationsList;
        List<IMKOverlay> overlaysList;

        MKMapItem sourceMapItem;
        MKMapItem destinationMapItem;
        IMKOverlay currentRouteOverlay;

        private UIGestureRecognizer longPressGestureRecognizer;

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

            longPressGestureRecognizer = new UILongPressGestureRecognizer(OnMapLongPress);

            InitLocationManager();
        }

        void InitLocationManager()
        {
            locMgr = new CLLocationManager();
            locMgr.DesiredAccuracy = 1; //accurate to 1 meter
            locMgr.PausesLocationUpdatesAutomatically = false;

            // iOS 8 has additional permissions requirements
            if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
            {
                locMgr.RequestAlwaysAuthorization(); // works in background
                //locMgr.RequestWhenInUseAuthorization (); // only in foreground
            }

            if (UIDevice.CurrentDevice.CheckSystemVersion(9, 0))
            {
                locMgr.AllowsBackgroundLocationUpdates = true;
            }

            locMgr.AuthorizationChanged += OnLocMgrAuthChanged;
            locMgr.LocationsUpdated += OnLocMgrLocationsUpdated;

            locMgr.StartUpdatingLocation();
        }

        protected override void OnElementChanged(Xamarin.Forms.Platform.iOS.ElementChangedEventArgs<View> e)
        {
            base.OnElementChanged(e);

            if(e.OldElement != null)
            {
                NativeMapView.OverlayRenderer = null;
                NativeMapView.DidUpdateUserLocation -= OnUserLocationUpdated;
                NativeMapView.GetViewForAnnotation -= GetViewForAnnotation;
                NativeMapView.RemoveGestureRecognizer(longPressGestureRecognizer);

                CustomMap oldCustomMap = e.OldElement as CustomMap;
                oldCustomMap.CalculateRouteFromUserLocationNativeHandler -= CalculateRouteFromUserLocation;
                oldCustomMap.CalculateRouteNativeHandler -= CalculateRoute;
                oldCustomMap.ClearRouteNativeHandler -= ClearRoute;
                oldCustomMap.CenterOnUsersLocationNativeHandler -= CenterOnUsersLocation;
                oldCustomMap.SearchLocalNativeHandler -= SearchLocal;
            }

            if(e.NewElement != null)
            {
                NativeMapView.OverlayRenderer = GetOverlayRenderer;
                NativeMapView.DidUpdateUserLocation += OnUserLocationUpdated;
                NativeMapView.GetViewForAnnotation += GetViewForAnnotation;
                NativeMapView.AddGestureRecognizer(longPressGestureRecognizer);

                CustomMap newCustomMap = e.NewElement as CustomMap;
                newCustomMap.CalculateRouteFromUserLocationNativeHandler += CalculateRouteFromUserLocation;
                newCustomMap.CalculateRouteNativeHandler += CalculateRoute;
                newCustomMap.ClearRouteNativeHandler += ClearRoute;
                newCustomMap.CenterOnUsersLocationNativeHandler += CenterOnUsersLocation;
                newCustomMap.SearchLocalNativeHandler += SearchLocal;

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

        void OnMapLongPress()
        {
            if (longPressGestureRecognizer.State != UIGestureRecognizerState.Began) 
                return;

            CGPoint pixelLocation = longPressGestureRecognizer.LocationInView(NativeMapView);
            CLLocationCoordinate2D coordinate = NativeMapView.ConvertPoint(pixelLocation, NativeMapView);

            //add map annotation
            MKPointAnnotation annotation = new MKPointAnnotation();
            annotation.Coordinate = coordinate;
            annotation.Title = string.Format("{0},{1}", coordinate.Latitude, coordinate.Longitude);

            NativeMapView.AddAnnotation(annotation);
            annotationsList.Add(annotation);
        }

        void OnLocMgrAuthChanged(object sender, CLAuthorizationChangedEventArgs e)
        {
            if(e.Status == CLAuthorizationStatus.Denied)
            {
                CustomMap.LocationAuthStatus = CustomMap.LocAuthStatus.NotAllowed;
            }
            else if (e.Status == CLAuthorizationStatus.AuthorizedWhenInUse)
            {
                CustomMap.LocationAuthStatus = CustomMap.LocAuthStatus.AllowedInForeground;
            }
            else if (e.Status == CLAuthorizationStatus.AuthorizedAlways)
            {
                CustomMap.LocationAuthStatus = CustomMap.LocAuthStatus.AllowedInBackground;
            }
        }

        void OnLocMgrLocationsUpdated(object sender, CLLocationsUpdatedEventArgs e)
        {
            //if (CustomMapView != null && CustomMapView.IsNavigating)
            //{
            //    CLLocation newLocation = e.Locations[e.Locations.Length - 1];

            //    if (lastUserLocation != null)
            //    {
            //        double dist = newLocation.DistanceFrom(lastUserLocation);

            //        if (dist > 10)
            //        {
            //            //TODO: podonnell: There is an issue on the simulator whereby calculating the route details resets the user location
            //            //it is then updated again by the location manager which calls this method again and results in an infinite loop
            //            System.Diagnostics.Debug.WriteLine(dist);

            //            //we have moved over 10m => update route
            //            sourceMapItem = MKMapItem.MapItemForCurrentLocation();
            //            CalculateRouteDetails(false);
            //        }
            //    }

            //    lastUserLocation = newLocation;
            //}
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
                MKCoordinateRegion region = MKCoordinateRegion.FromDistance(coords, 2000, 2000);
                NativeMapView.SetRegion(region, true);
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

        void CalculateRouteFromUserLocation(object sender, EventArgs e)
        {
            CLLocationCoordinate2D destinationLocation = new CLLocationCoordinate2D(CustomMapView.RouteDestination.Latitude, CustomMapView.RouteDestination.Longitude);
            MKPlacemark destinationPlacemark = new MKPlacemark(coordinate: destinationLocation);

            sourceMapItem = MKMapItem.MapItemForCurrentLocation();
            destinationMapItem = new MKMapItem(destinationPlacemark);

            MKPointAnnotation destinationAnnotation = new MKPointAnnotation();
            //destinationAnnotation.Title = "Destination";
            destinationAnnotation.Coordinate = destinationPlacemark.Location.Coordinate;

            //save annotations so they can be removed later if requested
            annotationsList.Add(destinationAnnotation);

            NativeMapView.AddAnnotation(destinationAnnotation);

            CalculateRouteDetails();
        }

        void CalculateRoute(object sender, EventArgs e)
        {
            CLLocationCoordinate2D sourceLocation = new CLLocationCoordinate2D(CustomMapView.RouteSource.Latitude, CustomMapView.RouteSource.Longitude);
            CLLocationCoordinate2D destinationLocation = new CLLocationCoordinate2D(CustomMapView.RouteDestination.Latitude, CustomMapView.RouteDestination.Longitude);

            MKPlacemark sourcePlacemark = new MKPlacemark(coordinate: sourceLocation);
            MKPlacemark destinationPlacemark = new MKPlacemark(coordinate: destinationLocation);

            sourceMapItem = new MKMapItem(sourcePlacemark);
            destinationMapItem = new MKMapItem(destinationPlacemark);
                    
            MKPointAnnotation sourceAnnotation = new MKPointAnnotation();
            //sourceAnnotation.Title = "Source";
            sourceAnnotation.Coordinate = sourcePlacemark.Location.Coordinate;

            MKPointAnnotation destinationAnnotation = new MKPointAnnotation();
            //destinationAnnotation.Title = "Destination";
            destinationAnnotation.Coordinate = destinationPlacemark.Location.Coordinate;

            //save annotations so they can be removed later if requested
            annotationsList.Add(sourceAnnotation);
            annotationsList.Add(destinationAnnotation);

            NativeMapView.AddAnnotation(sourceAnnotation);
            NativeMapView.AddAnnotation(destinationAnnotation);

            CalculateRouteDetails();
        }

        void CalculateRouteDetails(bool zoomMapToShowFullRoute = true, bool requestAlternateRoutes = true, MKDirectionsTransportType transportType = MKDirectionsTransportType.Automobile)
        {
            MKDirectionsRequest directionRequest = new MKDirectionsRequest();
            directionRequest.Source = sourceMapItem;
            directionRequest.Destination = destinationMapItem;
            directionRequest.RequestsAlternateRoutes = requestAlternateRoutes;
            directionRequest.TransportType = transportType;

            MKDirections eta = new MKDirections(directionRequest);

            eta.CalculateETA((MKETAResponse response, Foundation.NSError error) =>
            {
                if (error != null)
                {
                    System.Diagnostics.Debug.WriteLine(error.Description);
                }
                else
                {                    
                    TimeSpan time = TimeSpan.FromSeconds(response.ExpectedTravelTime);
                    System.Diagnostics.Debug.WriteLine(time.ToString(@"hh\:mm\:ss\:fff"));
                }
            });

            MKDirections directions = new MKDirections(directionRequest);


            directions.CalculateDirections((MKDirectionsResponse response, Foundation.NSError error) =>
            {                
                if (error != null)
                {
                    System.Diagnostics.Debug.WriteLine(error.Description);
                }
                else
                {
                    //remove previous route overlays
                    ClearOverlays();

                    currentRouteOverlay = response.Routes[0].Polyline;

                    MKRoute route;

                    //loop through backwards so first most route is renderered on top
                    for (int i = response.Routes.Length - 1; i >= 0; i--)
                    {
                        route = response.Routes[i];

                        //save overlay so it can be removed later if requested
                        overlaysList.Add(route.Polyline);

                        NativeMapView.AddOverlay(route.Polyline, MKOverlayLevel.AboveRoads);

                        if (i == 0)
                        {
                            if(zoomMapToShowFullRoute)
                            {
                                MKMapRect rect = route.Polyline.BoundingMapRect;
                                MKMapRect expandedRect = NativeMapView.MapRectThatFits(rect, new UIEdgeInsets(20, 20, 20, 20));
                                
                                NativeMapView.SetRegion(MKCoordinateRegion.FromMapRect(expandedRect), true);                            
                            }
                        }
                    }
                }
            });
        }

        void ClearRoute(object sender, EventArgs e)
        {
            CustomMapView.IsNavigating = false;

            ClearAnnotations();
            ClearOverlays();
        }

        void ClearAnnotations()
        {
            if(annotationsList.Count > 0)
            {
                NativeMapView.RemoveAnnotations(annotationsList.ToArray());
                annotationsList.Clear();                
            }
        }

        void ClearOverlays()
        {
            if(overlaysList.Count > 0)
            {
                NativeMapView.RemoveOverlays(overlaysList.ToArray());
                overlaysList.Clear();                
            }

            currentRouteOverlay = null;
        }

        MKOverlayRenderer GetOverlayRenderer(MKMapView mapView, IMKOverlay overlay)
        {            
            MKPolylineRenderer polylineRenderer = new MKPolylineRenderer(overlay as MKPolyline);

            if(currentRouteOverlay == overlay)
            {
                polylineRenderer.StrokeColor = RouteColor;
            }
            else
            {
                polylineRenderer.StrokeColor = AltRouteColor;                                
            }

            polylineRenderer.LineWidth = 4;

            return polylineRenderer;
        }

        MKAnnotationView GetViewForAnnotation(MKMapView mapView, IMKAnnotation annotation)
        {
            if (IsUserLocationAnnotation(mapView, annotation))
                return null;

            MKPinAnnotationView pinView = NativeMapView.DequeueReusableAnnotation(DefaultPinId) as MKPinAnnotationView;

            if(pinView == null)
            {
                pinView = new MKPinAnnotationView(annotation, DefaultPinId);
            }

            pinView.CanShowCallout = true;
            pinView.AnimatesDrop = true;

            UIButton button = new UIButton(UIButtonType.ContactAdd);
            button.TouchUpInside += (object sender, EventArgs e) => 
            {
                sourceMapItem = MKMapItem.MapItemForCurrentLocation();
                destinationMapItem = new MKMapItem(new MKPlacemark(annotation.Coordinate));
            
                CalculateRouteDetails();
            };

            pinView.RightCalloutAccessoryView = button;

            return pinView;
        }

        private bool IsUserLocationAnnotation(MKMapView mapView, IMKAnnotation annotation)
        {
            var userLocationAnnotation = ObjCRuntime.Runtime.GetNSObject(annotation.Handle) as MKUserLocation;
            if (userLocationAnnotation != null)
            {
                return userLocationAnnotation == mapView.UserLocation;
            }

            return false;
        }

        void SearchLocal(object sender, EventArgs e)
        {
            MKLocalSearchRequest request = new MKLocalSearchRequest();
            request.NaturalLanguageQuery = CustomMapView.SearchText;
            request.Region = NativeMapView.Region;

            MKLocalSearch search = new MKLocalSearch(request);

            search.Start((MKLocalSearchResponse response, Foundation.NSError error) =>
            {
                if(error != null)
                {
                    System.Diagnostics.Debug.WriteLine(error.Description);
                }
                else if(response.MapItems.Length == 0)                    
                {
                    System.Diagnostics.Debug.WriteLine("No matches found for search: " + CustomMapView.SearchText);
                }
                else
                {
                    ClearRoute(null, null);

                    foreach(MKMapItem mapItem in response.MapItems)
                    {
                        MKPointAnnotation annotation = new MKPointAnnotation();
                        annotation.Coordinate = mapItem.Placemark.Coordinate;
                        annotation.Title = mapItem.Name;

                        annotationsList.Add(annotation);
                    }

                    NativeMapView.ShowAnnotations(annotationsList.ToArray(), true);
                }
            });                
        }
   }
}
