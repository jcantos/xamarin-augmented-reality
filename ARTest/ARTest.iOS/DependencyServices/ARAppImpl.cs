using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ARTest.Interfaces;
using ARTest.iOS.AR;
using Foundation;
using UIKit;

[assembly: Xamarin.Forms.Dependency(typeof(ARTest.iOS.DependencyServices.ARAppImpl))]
namespace ARTest.iOS.DependencyServices
{
    public class ARAppImpl : IARApp
    {
        public void LaunchAR()
        {
            // This is in native code; invoke the native UI
            ARViewController viewController = new ARViewController();
            UIApplication.SharedApplication.KeyWindow.RootViewController.
              PresentViewController(viewController, true, null);
        }
    }
}