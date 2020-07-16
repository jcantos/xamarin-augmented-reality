using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Media;
using Android.Opengl;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Util;
using Android.Util;
using Android.Views;
using Android.Widget;
using Google.AR.Core;
using Google.AR.Core.Exceptions;
using Google.AR.Sceneform;
using Google.AR.Sceneform.Math;
using Google.AR.Sceneform.Rendering;
using Google.AR.Sceneform.UX;
using Java.Util;
using Java.Util.Concurrent.Atomic;
using Javax.Microedition.Khronos.Opengles;
using Plugin.Permissions;

namespace ARTest.Droid.AR
{
    [Activity(Label = "ARViewActivity")]
    public class ARViewActivity : FragmentActivity, Google.AR.Sceneform.Scene.IOnUpdateListener, BaseArFragment.IOnTapArPlaneListener
    {
        public static ARViewActivity Instance;

        const string TAG = "AR-TEST";
        double MIN_OPENGL_VERSION = 3.0;

        ArFragment arFragment;
        AnchorNode currentAnchorNode;
        TextView tvDistance;
        ModelRenderable cubeRenderable;
        Anchor currentAnchor = null;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Instance = this;

            if (!checkIsSupportedDeviceOrFinish())
            {
                Toast.MakeText(this, "Device not supported", ToastLength.Long).Show();
            }

            SetContentView(Resource.Layout.ar_view);
            arFragment = (ArFragment)SupportFragmentManager.FindFragmentById(Resource.Id.ux_fragment);
            tvDistance = (TextView)FindViewById(Resource.Id.tvDistance);

            initModel();

            arFragment.SetOnTapArPlaneListener(this);

        }
        protected override void OnResume()
        {

            base.OnResume();
        }

        public void OnUpdate(FrameTime frameTime)
        {
            Frame frame = arFragment.ArSceneView.ArFrame;

            if (currentAnchorNode != null)
            {
                Pose objectPose = currentAnchor.Pose;
                Pose cameraPose = frame.Camera.Pose;

                float dx = objectPose.Tx() - cameraPose.Tx();
                float dy = objectPose.Ty() - cameraPose.Ty();
                float dz = objectPose.Tz() - cameraPose.Tz();

                ///Compute the straight-line distance.
                float distanceMeters = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                tvDistance.Text = "Distance from camera: " + distanceMeters + " metres";

                /*float[] distance_vector = currentAnchor.getPose().inverse()
                        .compose(cameraPose).getTranslation();
                float totalDistanceSquared = 0;
                for (int i = 0; i < 3; ++i)
                    totalDistanceSquared += distance_vector[i] * distance_vector[i];*/
            }
        }

        private bool checkIsSupportedDeviceOrFinish()
        {
            var activityManager = (ActivityManager)Objects.RequireNonNull(this.GetSystemService(Context.ActivityService));
            string openGlVersionString = activityManager.DeviceConfigurationInfo.GlEsVersion;

            if (double.Parse(openGlVersionString) < MIN_OPENGL_VERSION)
            {
                Toast.MakeText(this, "Sceneform requires OpenGL ES 3.0 or later", ToastLength.Long).Show();
                this.Finish();
                return false;
            }

            return true;
        }

        private void initModel()
        {
            MaterialFactory
                .MakeTransparentWithColor(this, new Google.AR.Sceneform.Rendering.Color(Android.Graphics.Color.Red))
                .ThenAccept(new MaterialConsumer((material) =>
                {
                    Vector3 vector3 = new Vector3(0, 0, 0);
                    cubeRenderable = ShapeFactory.MakeSphere(0.025f, vector3, material);
                    cubeRenderable.ShadowCaster = false;
                    cubeRenderable.ShadowReceiver = false;
                }));
        }

        public void OnTapPlane(HitResult hitResult, Plane plane, MotionEvent motionEvent)
        {
            if (cubeRenderable == null)
                return;

            // creating Anchor
            Anchor anchor = hitResult.CreateAnchor();
            AnchorNode anchorNode = new AnchorNode(anchor);
            anchorNode.SetParent(arFragment.ArSceneView.Scene);

            clearAnchor();

            currentAnchor = anchor;
            currentAnchorNode = anchorNode;

            TransformableNode node = new TransformableNode(arFragment.TransformationSystem);
            node.Renderable = cubeRenderable;
            node.SetParent(anchorNode);
            arFragment.ArSceneView.Scene.AddOnUpdateListener(this);
            arFragment.ArSceneView.Scene.AddChild(anchorNode);
            node.Select();
        }

        private void clearAnchor()
        {
            currentAnchor = null;

            if (currentAnchorNode != null)
            {
                arFragment.ArSceneView.Scene.RemoveChild(currentAnchorNode);
                currentAnchorNode.Anchor.Detach();
                currentAnchorNode.SetParent(null);
                currentAnchorNode = null;
            }
        }

        internal class MaterialConsumer : Java.Lang.Object, Java.Util.Functions.IConsumer
        {
            Action<Material> _completed;

            public MaterialConsumer(Action<Material> action)
            {
                _completed = action;
            }

            public void Accept(Java.Lang.Object t)
            {
                _completed(t.JavaCast<Material>());
            }
        }

    }
}