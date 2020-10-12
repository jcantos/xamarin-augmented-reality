using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
using ARTest.Models;
using Google.AR.Core;
using Google.AR.Core.Exceptions;
using Google.AR.Sceneform;
using Google.AR.Sceneform.Math;
using Google.AR.Sceneform.Rendering;
using Google.AR.Sceneform.UX;
using Java.IO;
using Java.Nio;
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
        ModelRenderable cubeRenderable;
        Anchor currentAnchor = null;

        ImageView screenshot;
        ImageView pizarra;
        TextView lblDistanciaRealTime;
        Button btnCalcular;
        Button btnContinuar;
        Button btnMedicionCopa;
        TextView lblAyuda;
        TextView lblMidiendo;
        TextView lblH;
        TextView lblD;

        MedidasAR medidas;
        int numberMeditions = 0;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Instance = this;

            if (!checkIsSupportedDeviceOrFinish())
            {
                Toast.MakeText(this, "Dispositivo no soportado", ToastLength.Long).Show();
            }

            medidas = new MedidasAR();
            SetContentView(Resource.Layout.ar_view);
            arFragment = (ArFragment)SupportFragmentManager.FindFragmentById(Resource.Id.ux_fragment);
            lblDistanciaRealTime = (TextView)FindViewById(Resource.Id.lblDistanciaRealTime);
            btnCalcular = (Button)FindViewById(Resource.Id.btnCalcular);
            btnContinuar = (Button)FindViewById(Resource.Id.btnContinuar);
            btnMedicionCopa = (Button)FindViewById(Resource.Id.btnMedicionCopa);
            lblAyuda = (TextView)FindViewById(Resource.Id.lblAyuda);
            lblMidiendo = (TextView)FindViewById(Resource.Id.lblMidiendo);
            lblH = (TextView)FindViewById(Resource.Id.lblH);
            lblD = (TextView)FindViewById(Resource.Id.lblD);
            screenshot = (ImageView)FindViewById(Resource.Id.screenshot);
            pizarra = (ImageView)FindViewById(Resource.Id.pizarra);

            initModel();

            arFragment.SetOnTapArPlaneListener(this);
        }
        protected override void OnResume()
        {
            base.OnResume();

            setBindings();
            setFrames();
            setTextoAyuda("Acérquese al olivo y seleccione el pie");
        }
        private void setBindings()
        {
            btnMedicionCopa.Click += ((IntentSender, arg) =>
            {
                RunOnUiThread(() =>
                {
                    var image = arFragment.ArSceneView.ArFrame.AcquireCameraImage();

                    var cameraPlaneY = image.GetPlanes()[0].Buffer;
                    var cameraPlaneU = image.GetPlanes()[1].Buffer;
                    var cameraPlaneV = image.GetPlanes()[2].Buffer;

                    var compositeByteArray = new byte[cameraPlaneY.Capacity() + cameraPlaneU.Capacity() + cameraPlaneV.Capacity()];

                    cameraPlaneY.Get(compositeByteArray, 0, cameraPlaneY.Capacity());
                    cameraPlaneU.Get(compositeByteArray, cameraPlaneY.Capacity(), cameraPlaneU.Capacity());
                    cameraPlaneV.Get(compositeByteArray, cameraPlaneY.Capacity() + cameraPlaneU.Capacity(), cameraPlaneV.Capacity());

                    var baOutputStream = new MemoryStream();
                    var yuvImage = new YuvImage(compositeByteArray, ImageFormatType.Nv21, image.Width, image.Height, null);
                    yuvImage.CompressToJpeg(new Rect(0, 0, image.Width, image.Height), 75, baOutputStream);
                    var byteForBitmap = baOutputStream.ToArray();
                    var bitmapImage = BitmapFactory.DecodeByteArray(byteForBitmap, 0, byteForBitmap.Length);

                    screenshot.SetImageBitmap(bitmapImage);
                    screenshot.Visibility = ViewStates.Visible;
                    pizarra.Visibility = ViewStates.Visible;
                    pizarra.SetImageBitmap(null);

                    var ft = SupportFragmentManager.BeginTransaction();
                    ft.Hide(arFragment);

                    btnMedicionCopa.Visibility = ViewStates.Visible;

                    if (numberMeditions == 0)
                        setTextoAyuda("Ahora indique H1");
                    else if (numberMeditions == 2)
                        setTextoAyuda("Ahora indique H2");
                });
            });

            btnCalcular.Click += ((sender, arg) =>
            {
                if (medidas == null)
                    return;

                //ARService.Current.OnCapturarMedidas(medidas);
                // cerrar ventana
            });
        }

        private void setFrames()
        {

        }
        private void setTextoAyuda(string texto)
        {
            RunOnUiThread(() => {
                this.lblAyuda.Text = texto;
            });
        }
        private void habilitarMedicionCopa()
        {
            RunOnUiThread(() =>
            {
                btnMedicionCopa.Visibility = ViewStates.Visible;
                lblDistanciaRealTime.Visibility = ViewStates.Visible;
            });
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
                double distanceMeters = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                distanceMeters = Math.Round(distanceMeters, 2);
                lblDistanciaRealTime.Text = "Distancia: " + distanceMeters + " m";
            }
        }

        private bool checkIsSupportedDeviceOrFinish()
        {
            var activityManager = (ActivityManager)Objects.RequireNonNull(this.GetSystemService(Context.ActivityService));
            string openGlVersionString = activityManager.DeviceConfigurationInfo.GlEsVersion;

            if (double.Parse(openGlVersionString) < MIN_OPENGL_VERSION)
            {
                Toast.MakeText(this, "Sceneform requiere OpenGL ES 3.0 o superior", ToastLength.Long).Show();
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

            habilitarMedicionCopa();
            setTextoAyuda("Aléjese hasta encuadrar olivo y pulse 'Medir copa'");
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