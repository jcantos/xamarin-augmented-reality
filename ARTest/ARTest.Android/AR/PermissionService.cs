using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;

namespace ARTest.Droid.AR
{
    public class PermissionService
    {
        static PermissionService permissionService;

        public static PermissionService Current
        {
            get
            {
                if (permissionService == null)
                {
                    permissionService = new PermissionService();
                }

                return permissionService;
            }
        }

        public async Task<bool> CheckPermission()
        {
            bool reply = false;

            try
            {
                var status = await CrossPermissions.Current.CheckPermissionStatusAsync<CameraPermission>();
                if (status == PermissionStatus.Granted)
                {
                    reply = true;
                }
                else
                {
                    if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(Permission.Camera))
                    {
                        Toast.MakeText(MainActivity.Instance, "Es necesario acceder a la cámara", ToastLength.Long);
                    }

                    Permission[] permissions = new Permission[1];
                    permissions[0] = Permission.Camera;
                    var permissionsResult = await CrossPermissions.Current.RequestPermissionAsync<CameraPermission>();

                    if (permissionsResult == PermissionStatus.Granted)
                        reply = true;
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(MainActivity.Instance, ex.Message, ToastLength.Long);
            }

            return reply;
        }

    }
}