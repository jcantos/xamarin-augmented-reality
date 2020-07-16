using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using ARTest.Droid.AR;
using ARTest.Interfaces;
using Xamarin.Forms;

[assembly: Xamarin.Forms.Dependency(typeof(ARTest.Droid.DependencyServices.ARAppImpl))]
namespace ARTest.Droid.DependencyServices
{
    public class ARAppImpl : IARApp
    {
        public void LaunchAR()
        {
            var intent = new Intent(MainActivity.Instance, typeof(ARViewActivity));
            //intent.SetFlags(ActivityFlags.NewTask);
            MainActivity.Instance.StartActivity(intent);
        }
    }
}