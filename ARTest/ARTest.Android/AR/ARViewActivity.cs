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
using Android.Graphics.Drawables;
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
        Pizarra pizarra;
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
        int numTaps = 0;
        bool pizarraEnabled;
        Bitmap imagenPizarra;

        Android.Graphics.Point pointA;
        Android.Graphics.Point pointB;

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
            pizarra = (Pizarra)FindViewById(Resource.Id.pizarra);

            initModel();

            arFragment.SetOnTapArPlaneListener(this);
        }
        protected override void OnResume()
        {
            base.OnResume();

            setBindings();
            setFrames();
            setTextoAyuda("Acérquese al olivo y seleccione el pie");
            setTapPizarra();
        }
        private void setBindings()
        {
            btnMedicionCopa.Click += ((IntentSender, arg) =>
            {
                RunOnUiThread(() =>
                {
                    var image = arFragment.ArSceneView.ArFrame.AcquireCameraImage();
                    arFragment.ArSceneView.Pause();

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

                    var matrix = new Android.Graphics.Matrix();
                    matrix.PostRotate(90);
                    var rotateImage = Bitmap.CreateBitmap(bitmapImage, 0, 0, bitmapImage.Width, bitmapImage.Height, matrix, true);

                    imagenPizarra = rotateImage;
                    screenshot.SetImageBitmap(rotateImage);
                    screenshot.Visibility = ViewStates.Visible;
                    pizarra.Visibility = ViewStates.Visible;
                    pizarra.SetImageBitmap(null);

                    var ft = SupportFragmentManager.BeginTransaction();
                    ft.Hide(arFragment);

                    btnMedicionCopa.Visibility = ViewStates.Gone;

                    if (numberMeditions == 0)
                        setTextoAyuda("Ahora indique H1");
                    else if (numberMeditions == 2)
                        setTextoAyuda("Ahora indique H2");
                });
            });

            btnContinuar.Click += ((sender, arg) =>
            {
                RunOnUiThread(() =>
                {
                    screenshot.Visibility = ViewStates.Gone;
                    pizarra.Visibility = ViewStates.Gone;

                    var ft = SupportFragmentManager.BeginTransaction();
                    ft.Show(arFragment);
                    arFragment.ArSceneView.Resume();

                    btnMedicionCopa.Visibility = ViewStates.Visible;
                    setTextoAyuda("Sitúese en perpendicular y pulse 'Medir copa'");
                    clearPizarra();
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

        private void clearPizarra()
        {
            btnContinuar.Visibility = ViewStates.Gone;
            btnCalcular.Visibility = ViewStates.Gone;
            lblH.Visibility = ViewStates.Gone;
            lblD.Visibility = ViewStates.Gone;
            pizarraEnabled = true;

            pizarra.TypeDraw = "";
            pizarra.PointA = null;
            pizarra.PointB = null;
            pizarra.SetImageBitmap(null);
        }

        private void setFrames()
        {
            var lpDistanciaRealTime = (RelativeLayout.LayoutParams)lblDistanciaRealTime.LayoutParameters;
            lpDistanciaRealTime.TopMargin = 80;
            lblDistanciaRealTime.LayoutParameters = lpDistanciaRealTime;

            var lpBtnCalcular = (RelativeLayout.LayoutParams)btnCalcular.LayoutParameters;
            lpBtnCalcular.TopMargin = Resources.DisplayMetrics.HeightPixels - 165;
            btnCalcular.LayoutParameters = lpBtnCalcular;

            var lpBtnMedicionCopa = (RelativeLayout.LayoutParams)btnMedicionCopa.LayoutParameters;
            lpBtnMedicionCopa.TopMargin = Resources.DisplayMetrics.HeightPixels - 165;
            btnMedicionCopa.LayoutParameters = lpBtnMedicionCopa;

            var lpBtnContinuar = (RelativeLayout.LayoutParams)btnContinuar.LayoutParameters;
            lpBtnContinuar.TopMargin = Resources.DisplayMetrics.HeightPixels - 165;
            btnContinuar.LayoutParameters = lpBtnContinuar;

            var lpH = (RelativeLayout.LayoutParams)lblH.LayoutParameters;
            lpH.TopMargin = 180;
            lblH.LayoutParameters = lpH;

            var lpD = (RelativeLayout.LayoutParams)lblD.LayoutParameters;
            lpD.TopMargin = 250;
            lblD.LayoutParameters = lpD;

            var lpMidiendo = (RelativeLayout.LayoutParams)lblMidiendo.LayoutParameters;
            lpMidiendo.TopMargin = 80;
            lblMidiendo.LayoutParameters = lpMidiendo;

            lblDistanciaRealTime.SetBackgroundColor(Android.Graphics.Color.ParseColor("#FFCE45"));
            lblDistanciaRealTime.SetTextColor(Android.Graphics.Color.ParseColor("#000000"));

            lblH.SetBackgroundColor(Android.Graphics.Color.ParseColor("#FFCE45"));
            lblH.SetTextColor(Android.Graphics.Color.ParseColor("#000000"));

            lblD.SetBackgroundColor(Android.Graphics.Color.ParseColor("#FFCE45"));
            lblD.SetTextColor(Android.Graphics.Color.ParseColor("#000000"));

            lblMidiendo.SetBackgroundColor(Android.Graphics.Color.ParseColor("#FFCE45"));
            lblMidiendo.SetTextColor(Android.Graphics.Color.ParseColor("#000000"));

            lblAyuda.SetBackgroundColor(Android.Graphics.Color.ParseColor("#FB5C00"));
            lblAyuda.SetTextColor(Android.Graphics.Color.ParseColor("#ffffff"));
        }
        private void setTapPizarra()
        {
            pizarraEnabled = true;

            pizarra.Touch += ((sender, arg) =>
            {
                if (!pizarraEnabled)
                    return;

                if (arg.Event.Action == MotionEventActions.Down)
                {
                    numTaps++;

                    var touchX = (int)arg.Event.GetX();
                    var touchY = (int)arg.Event.GetY();
                    var posX = touchX;
                    var posY = touchY;

                    if (numTaps == 1)
                    {
                        setVisibilityMidiendo(true);
                        pointA = new Android.Graphics.Point((int)posX, (int)posY);
                        pizarra.PointA = pointA;
                        pizarra.TypeDraw = "circle";
                        pizarra.Invalidate();
                    }
                    else if (numTaps == 2)
                    {
                        setVisibilityMidiendo(false);
                        pointB = new Android.Graphics.Point((int)posX, (int)posY);
                        numTaps = 0;
                        numberMeditions++;

                        pizarra.PointA = pointA;
                        pizarra.PointB = pointB;
                        pizarra.TypeDraw = "line";
                        pizarra.Invalidate();

                        var distance = calculateDistance(pointA, pointB, medidas.DistanciaAlArbol);
                        addDistanceText(distance);
                    }
                }

            });
        }
        private void drawPointPizarra(Android.Graphics.Point point)
        {
            if (imagenPizarra == null)
                return;

            Paint paint = new Paint();
            paint.AntiAlias = true;
            paint.Color = Android.Graphics.Color.Red;

            Bitmap bitmap = ((BitmapDrawable)screenshot.Drawable).Bitmap.Copy(Bitmap.Config.Argb8888, true);

            Canvas canvas = new Canvas(bitmap);
            canvas.DrawCircle(point.X, point.Y, 10, paint);

            imagenPizarra = bitmap;
            pizarra.SetImageBitmap(bitmap);
        }
        private void setTextoAyuda(string texto)
        {
            RunOnUiThread(() =>
            {
                this.lblAyuda.Text = texto;
            });
        }
        private void setVisibilityMidiendo(bool visible)
        {
            RunOnUiThread(() =>
            {
                if (visible)
                {
                    lblMidiendo.Text = "Pulse para indicar punto final ...";
                    lblMidiendo.Visibility = ViewStates.Visible;
                }
                else
                {
                    lblMidiendo.Text = "";
                    lblMidiendo.Visibility = ViewStates.Gone;
                }
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
        private double calculateDistance(Android.Graphics.Point pointA, Android.Graphics.Point pointB, double distanciaAlArbolEnMetros)
        {
            const double CALIBRACION = 0.0616;
            double reply = 0;

            if (pointA == null)
                return 0;
            if (pointB == null)
                return 0;

            var x0 = pointA.X;
            var x1 = pointB.X;
            var y0 = pointA.Y;
            var y1 = pointB.Y;

            reply = Math.Sqrt(Math.Pow(x1 - x0, 2) + Math.Pow(y1 - y0, 2)); // distancia en pixels entre punto A y B
            reply = reply * distanciaAlArbolEnMetros;
            reply = reply * CALIBRACION; // conversión a metros

            return reply;
        }

        private void addDistanceText(double distance)
        {
            string distanceString = Math.Round((distance), 2) + " m";

            RunOnUiThread(() =>
            {
                double _distance = Math.Round(distance, 2);
                if (numberMeditions == 1)
                {
                    medidas.CopaH = distance;
                    lblH.Text = "H1: " + distanceString;
                    lblH.Visibility = ViewStates.Visible;
                    setTextoAyuda("Ahora indique D1");
                }
                else if (numberMeditions == 2)
                {
                    medidas.CopaD1 = distance;
                    lblD.Text = "D1: " + distanceString;
                    lblD.Visibility = ViewStates.Visible;
                    pizarraEnabled = false;
                    btnContinuar.Visibility = ViewStates.Visible;
                    setTextoAyuda("Pulse botón Continuar para medir D2");
                }
                else if (numberMeditions == 3)
                {
                    medidas.CopaH = (distance + medidas.CopaH) / 2;
                    lblH.Text = "H2: " + distanceString;
                    lblH.Visibility = ViewStates.Visible;
                    setTextoAyuda("Ahora indique D2");
                }
                else if (numberMeditions == 4)
                {
                    medidas.CopaD2 = distance;
                    lblD.Text = "D2: " + distanceString;
                    lblD.Visibility = ViewStates.Visible;
                    pizarraEnabled = false;
                    btnCalcular.Visibility = ViewStates.Visible;
                    setTextoAyuda("¡Listo!, pulse sobre el botón Calcular");
                }
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

                medidas.DistanciaAlArbol = distanceMeters;
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

        public class Pizarra : ImageView
        {
            public Android.Graphics.Point PointA { get; set; }
            public Android.Graphics.Point PointB { get; set; }
            public string TypeDraw { get; set; }
            public Pizarra(Context context) : base(context, null)
            {
            }

            public Pizarra(Context context, IAttributeSet attributeSet) : base(context, attributeSet)
            {
            }

            protected override void OnDraw(Canvas canvas)
            {
                base.OnDraw(canvas);

                if (TypeDraw == "circle")
                {
                    if (PointA != null)
                    {
                        Paint paint = new Paint();
                        paint.AntiAlias = true;
                        paint.Color = Android.Graphics.Color.Red;

                        canvas.DrawCircle(PointA.X, PointA.Y, 25, paint);
                    }
                }
                else if (TypeDraw == "line")
                {
                    if (PointA != null && PointB != null)
                    {
                        Paint paint = new Paint();
                        paint.AntiAlias = true;
                        paint.Color = Android.Graphics.Color.Red;
                        paint.StrokeWidth = 10;

                        canvas.DrawLine(PointA.X, PointA.Y, PointB.X, PointB.Y, paint);
                    }
                }
            }
        }
    }
}