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
        }

        protected override void OnElementChanged(Xamarin.Forms.Platform.iOS.ElementChangedEventArgs<View> e)
        {
            base.OnElementChanged(e);

            if(e.OldElement != null)
            {
                NativeMapView.OverlayRenderer = null;

                CustomMap oldCustomMap = e.OldElement as CustomMap;
                oldCustomMap.CalculateRouteNativeHandler -= CalculateRoute;
                oldCustomMap.ClearRouteNativeHandler -= ClearRoute;
            }

            if(e.NewElement != null)
            {
                NativeMapView.OverlayRenderer = GetOverlayRenderer;

                CustomMap newCustomMap = e.NewElement as CustomMap;
                newCustomMap.CalculateRouteNativeHandler += CalculateRoute;
                newCustomMap.ClearRouteNativeHandler += ClearRoute;

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

                    MKMapRect rect = route.Polyline.BoundingMapRect;
                    MKMapRect expandedRect = nativeMapView.MapRectThatFits(rect, new UIEdgeInsets(20,20,20,20));

                    nativeMapView.SetRegion(MKCoordinateRegion.FromMapRect(expandedRect), true);
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
   }
}
