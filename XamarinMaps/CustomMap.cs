using System;
using Xamarin.Forms.Maps;
using Xamarin.Forms;

namespace XamarinMaps
{
    public class CustomMap : Map
    {
        //RouteSourceProperty 
        public static readonly BindableProperty RouteSourceProperty =
            BindableProperty.Create("RouteSource", typeof(Position), typeof(CustomMap), defaultValue: new Position(), propertyChanged: OnRouteSourceChanged);

        public Position RouteSource
        {
            get { return (Position)GetValue(RouteSourceProperty); }
            set { SetValue(RouteSourceProperty, value); }
        }

        public static void OnRouteSourceChanged(BindableObject bindable, object oldValue, object newValue)
        {
        }

        //RouteDestinationProperty 
        public static readonly BindableProperty RouteDestinationProperty =
        BindableProperty.Create("RouteDestination", typeof(Position), typeof(CustomMap), defaultValue: new Position(), propertyChanged: OnRouteDestinationChanged);

        public Position RouteDestination
        {
            get { return (Position)GetValue(RouteDestinationProperty); }
            set { SetValue(RouteDestinationProperty, value); }
        }

        public static void OnRouteDestinationChanged(BindableObject bindable, object oldValue, object newValue)
        {
        }

        //ShowTrafficProperty 
        public static readonly BindableProperty ShowTrafficProperty =
        BindableProperty.Create("ShowTraffic", typeof(bool), typeof(CustomMap), defaultValue: true, propertyChanged: OnShowTrafficChanged);

        public bool ShowTraffic
        {
            get { return (bool)GetValue(ShowTrafficProperty); }
            set { SetValue(ShowTrafficProperty, value); }
        }

        public static void OnShowTrafficChanged(BindableObject bindable, object oldValue, object newValue)
        {
        }

        public CustomMap()
        {            
        }

        public event EventHandler CalculateRouteNativeHandler;

        public void CalculateRoute(Position source, Position destination)
        {
            RouteSource = source;
            RouteDestination = destination;

            if(CalculateRouteNativeHandler != null)
            {
                CalculateRouteNativeHandler(this, EventArgs.Empty);
            }
        }

        public event EventHandler ClearRouteNativeHandler;

        public void ClearRoute()
        {
            if(ClearRouteNativeHandler != null)                
            {
                ClearRouteNativeHandler(this, EventArgs.Empty);
            }
        }
    }
}
