using System;
using System.Drawing;

using CoreFoundation;
using UIKit;
using Foundation;
using ARKit;
using SceneKit;
using System.Collections.Generic;
using System.Linq;
using CoreGraphics;
using ARTest.Models;
using Xamarin.Forms;

namespace ARTest.iOS.AR
{
    [Register("AR2ViewController")]
    public class AR2ViewController : UIViewController
    {
        private readonly ARSCNView sceneView;

        UILabel lblAyuda;
        UILabel lblDistancia;
        UILabel lblDistanciaRealTime;

        UIButton btnCalcular;
        UIButton btnMedicionCopa;
        UIButton btnContinuar;
        UIImageView screenshot;
        int numTaps = 0;
        int numberMeditions = 0;
        CGPoint pointA;
        CGPoint pointB;
        Pizarra pizarra;

        UILabel lblH;
        UILabel lblD;
        MedidasAR medidas;
        SCNNode markerNode;
        SceneViewDelegate sceneViewDelegate;

        public AR2ViewController()
        {
            medidas = new MedidasAR();

            this.screenshot = new UIImageView
            {
                ContentMode = UIViewContentMode.ScaleAspectFit,
                UserInteractionEnabled = false,
                Hidden = true
            };

            this.pizarra = new Pizarra
            {
                UserInteractionEnabled = true,
                Hidden = true
            };

            btnCalcular = new UIButton();
            btnCalcular.BackgroundColor = new UIColor(red: 0.00f, green: 0.49f, blue: 0.38f, alpha: 1.00f);
            btnCalcular.SetTitle("Calcular", UIControlState.Normal);
            btnCalcular.SetTitleColor(UIColor.White, UIControlState.Normal);
            btnCalcular.Hidden = true;
            btnCalcular.AddTarget(BtnCalcularEventHandler, UIControlEvent.TouchUpInside);

            btnContinuar = new UIButton();
            btnContinuar.BackgroundColor = new UIColor(red: 0.00f, green: 0.49f, blue: 0.38f, alpha: 1.00f);
            btnContinuar.SetTitle("Continuar", UIControlState.Normal);
            btnContinuar.SetTitleColor(UIColor.White, UIControlState.Normal);
            btnContinuar.Hidden = true;
            btnContinuar.AddTarget(BtnContinuarEventHandler, UIControlEvent.TouchUpInside);

            btnMedicionCopa = new UIButton();
            btnMedicionCopa.BackgroundColor = new UIColor(red: 0.00f, green: 0.49f, blue: 0.38f, alpha: 1.00f);
            btnMedicionCopa.SetTitle("Medir copa", UIControlState.Normal);
            btnMedicionCopa.SetTitleColor(UIColor.White, UIControlState.Normal);
            btnMedicionCopa.Hidden = true;
            btnMedicionCopa.AddTarget(BtnMedicionCopaEventHandler, UIControlEvent.TouchUpInside);

            lblAyuda = new UILabel();
            lblAyuda.TextColor = UIColor.White;
            lblAyuda.BackgroundColor = UIColor.Orange;
            lblAyuda.Font = UIFont.FromName("AppleSDGothicNeo-Bold", 16f);
            lblAyuda.TextAlignment = UITextAlignment.Center;
            lblAyuda.Hidden = false;

            lblDistancia = new UILabel();
            lblDistancia.TextColor = UIColor.Black;
            lblDistancia.BackgroundColor = UIColor.Yellow;
            lblDistancia.Font = UIFont.FromName("AppleSDGothicNeo-Bold", 16f);
            lblDistancia.Hidden = true;

            lblDistanciaRealTime = new UILabel();
            lblDistanciaRealTime.TextColor = UIColor.Black;
            lblDistanciaRealTime.BackgroundColor = UIColor.Yellow;
            lblDistanciaRealTime.Font = UIFont.FromName("AppleSDGothicNeo-Bold", 16f);
            lblDistanciaRealTime.TextAlignment = UITextAlignment.Center;
            lblDistanciaRealTime.Hidden = true;

            lblH = new UILabel();
            lblH.TextColor = UIColor.Black;
            lblH.BackgroundColor = UIColor.Yellow;
            lblH.Font = UIFont.FromName("AppleSDGothicNeo-Bold", 16f);
            lblH.Hidden = true;

            lblD = new UILabel();
            lblD.TextColor = UIColor.Black;
            lblD.BackgroundColor = UIColor.Yellow;
            lblD.Font = UIFont.FromName("AppleSDGothicNeo-Bold", 16f);
            lblD.Hidden = true;

            this.sceneView = new ARSCNView
            {
                DebugOptions = ARSCNDebugOptions.ShowFeaturePoints
            };
            sceneViewDelegate = new SceneViewDelegate(sceneView, lblDistanciaRealTime, medidas);
            this.sceneView.Delegate = sceneViewDelegate;

            this.screenshot.AddSubview(lblDistancia);
            this.screenshot.AddSubview(lblH);
            this.screenshot.AddSubview(lblD);
            this.sceneView.AddSubview(lblDistanciaRealTime);
            this.sceneView.AddSubview(btnMedicionCopa);
            this.View.AddSubview(this.screenshot);
            this.View.AddSubview(this.pizarra);
            this.View.AddSubview(this.sceneView);
            this.View.AddSubview(this.lblAyuda);
            this.View.AddSubview(btnContinuar);
            this.View.AddSubview(btnCalcular);
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            setFrames();
            setTextoAyuda("Acérquese al olivo y seleccione el pie");
            setTapSceneView();
            setTapPizarra();
        }

        private void setFrames()
        {
            this.sceneView.Frame = this.View.Frame;
            this.screenshot.Frame = this.View.Frame;
            this.pizarra.Frame = this.View.Frame;
            this.lblAyuda.Frame = new CGRect(0, 0, this.View.Frame.Width, 30);
            this.lblDistanciaRealTime.Frame = new CGRect(0, 30, this.View.Frame.Width, 30);
            this.btnCalcular.Frame = new CGRect(10, this.View.Frame.Height - 135, this.View.Frame.Width - 20, 58);
            this.btnMedicionCopa.Frame = new CGRect(10, this.View.Frame.Height - 135, this.View.Frame.Width - 20, 58);
            this.btnContinuar.Frame = new CGRect(10, this.View.Frame.Height - 135, this.View.Frame.Width - 20, 58);
            this.lblDistancia.Frame = new CGRect(10, 35, 180, 25);
            this.lblH.Frame = new CGRect(25, 60, 180, 25);
            this.lblD.Frame = new CGRect(25, 95, 180, 25);
        }
        private void setTapSceneView()
        {
            var tapSceneView = new UITapGestureRecognizer((args) =>
            {
                var point = args.LocationInView(sceneView);
                if (point == null)
                    return;

                var hitTestResults = sceneView.HitTest(point, ARHitTestResultType.FeaturePoint);
                if (hitTestResults == null)
                    return;

                ARHitTestResult hitTest = hitTestResults.FirstOrDefault();
                if (hitTest == null)
                    return;

                clearScene();
                addMarker(hitTest);
                habilitarMedicionCopa();
                setTextoAyuda("Aléjese hasta encuadrar olivo y pulse 'Medir copa'");
            });

            this.sceneView.AddGestureRecognizer(tapSceneView);
        }
        private void setTapPizarra()
        {
            var tapPizarra = new UITapGestureRecognizer((args) =>
            {
                numTaps++;

                if (numTaps == 1)
                {
                    pointA = args.LocationInView(args.View);
                }
                else if (numTaps == 2)
                {
                    pointB = args.LocationInView(args.View);
                    numTaps = 0;
                    numberMeditions++;

                    pizarra.DrawLine(pointA, pointB);

                    var distance = calculateDistance(pointA, pointB, medidas.DistanciaAlArbol);
                    addDistanceText(distance);
                }
            });

            this.pizarra.AddGestureRecognizer(tapPizarra);
        }

        private void setTextoAyuda(string texto)
        {
            InvokeOnMainThread(() =>
            {
                this.lblAyuda.Text = texto;
            });
        }

        private void clearScene()
        {
            foreach (var node in sceneView.Scene.RootNode.ChildNodes)
                node.RemoveFromParentNode();

            if (sceneViewDelegate != null)
                sceneViewDelegate.MarkerNode = null;
        }

        private void clearPizarra()
        {
            btnContinuar.Hidden = true;
            btnCalcular.Hidden = true;
            lblH.Hidden = true;
            lblD.Hidden = true;
            pizarra.UserInteractionEnabled = true;
        }

        private void addMarker(ARHitTestResult hitTestResult)
        {
            var geometry = SCNSphere.Create((nfloat)0.01);
            geometry.FirstMaterial.Diffuse.Contents = UIColor.Red;

            markerNode = SCNNode.Create();
            markerNode.Geometry = geometry;
            markerNode.Position = new SCNVector3(hitTestResult.WorldTransform.Column3.X,
                hitTestResult.WorldTransform.Column3.Y,
                hitTestResult.WorldTransform.Column3.Z);

            sceneView.Scene.RootNode.AddChildNode(markerNode);

            if (sceneViewDelegate != null)
                sceneViewDelegate.MarkerNode = markerNode;
        }

        private void habilitarMedicionCopa()
        {
            InvokeOnMainThread(() =>
            {
                btnMedicionCopa.Hidden = false;
            });
        }

        private double calculateDistance(CGPoint pointA, CGPoint pointB, double distanciaAlArbolEnMetros)
        {
            const double CALIBRACION = 0.002;
            double reply = 0;

            var x0 = pointA.X;
            var x1 = pointB.X;
            var y0 = pointA.Y;
            var y1 = pointB.Y;

            reply = Math.Sqrt(Math.Pow(x1 - x0, 2) + Math.Pow(y1 - y0, 2)); // distancia en pixels entre punto A y B
            reply = reply * distanciaAlArbolEnMetros * CALIBRACION; // conversión a metros

            return reply;
        }

        private void addDistanceText(double distance)
        {
            string distanceString = Math.Round((distance), 2) + " ms";

            InvokeOnMainThread(() =>
            {
                double _distance = Math.Round(distance, 2);
                if (numberMeditions == 1)
                {
                    medidas.CopaH = distance;
                    lblH.Text = "H1: " + distanceString;
                    lblH.Hidden = false;
                    setTextoAyuda("Ahora indique D1");
                }
                else if (numberMeditions == 2)
                {
                    medidas.CopaD1 = distance;
                    lblD.Text = "D1: " + distanceString;
                    lblD.Hidden = false;
                    pizarra.UserInteractionEnabled = false;
                    btnContinuar.Hidden = false;
                    setTextoAyuda("Pulse botón Continuar para medir D2");
                }
                else if (numberMeditions == 3)
                {
                    medidas.CopaH = (distance + medidas.CopaH) / 2;
                    lblH.Text = "H2: " + distanceString;
                    lblH.Hidden = false;
                    setTextoAyuda("Ahora indique D2");
                }
                else if (numberMeditions == 4)
                {
                    medidas.CopaD2 = distance;
                    lblD.Text = "D2: " + distanceString;
                    lblD.Hidden = false;
                    pizarra.UserInteractionEnabled = false;
                    btnCalcular.Hidden = false;
                    setTextoAyuda("¡Listo!, pulse sobre el botón Calcular");
                }
            });
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            sceneView.Session.Run(new ARWorldTrackingConfiguration
            {
                AutoFocusEnabled = true,
                PlaneDetection = ARPlaneDetection.None,
                LightEstimationEnabled = true,
                WorldAlignment = ARWorldAlignment.GravityAndHeading
            }, ARSessionRunOptions.ResetTracking | ARSessionRunOptions.RemoveExistingAnchors);
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);
            sceneView.Session.Pause();
        }

        public void BtnCalcularEventHandler(object sender, EventArgs e)
        {
            if (medidas == null)
                return;

            //ARService.Current.OnCapturarMedidas(medidas);
            this.DismissModalViewController(true);
        }

        public void BtnMedicionCopaEventHandler(object sender, EventArgs e)
        {
            InvokeOnMainThread(() =>
            {
                var image = sceneView.Snapshot();
                screenshot.Image = image;
                screenshot.Hidden = false;
                pizarra.Hidden = false;
                pizarra.Image = null;
                sceneView.Hidden = true;

                if (numberMeditions == 0)
                    setTextoAyuda("Ahora indique H1");
                else if(numberMeditions == 2)
                    setTextoAyuda("Ahora indique H2");
            });
        }

        public void BtnContinuarEventHandler(object sender, EventArgs e)
        {
            InvokeOnMainThread(() =>
            {
                screenshot.Hidden = true;
                pizarra.Hidden = true;
                sceneView.Hidden = false;
                setTextoAyuda("Sitúese en perpendicular y pulse 'Medir copa'");
                clearPizarra();
            });
        }

        class SceneViewDelegate : ARSCNViewDelegate
        {
            ARSCNView sceneView;
            UILabel lblDistancia;
            MedidasAR medidas;
            DateTime lastUpdated = DateTime.MinValue;

            public SCNNode MarkerNode { get; set; }

            public SceneViewDelegate(ARSCNView _sceneView, UILabel _lblDistancia, MedidasAR _medidas)
            {
                sceneView = _sceneView;
                lblDistancia = _lblDistancia;
                medidas = _medidas;
            }
            public override void Update(ISCNSceneRenderer renderer, double timeInSeconds)
            {
                if (MarkerNode == null)
                    return;

                var nodeFrom = sceneView.PointOfView.WorldPosition;
                var nodeTo = MarkerNode.WorldPosition;

                double distancia = calculateDistance(nodeFrom, nodeTo);
                distancia = Math.Round(distancia, 2);
                medidas.DistanciaAlArbol = distancia;

                var diferencia = DateTime.Now - lastUpdated;
                if (diferencia.TotalMilliseconds > 200)
                {
                    lastUpdated = DateTime.Now;

                    InvokeOnMainThread(() =>
                    {
                        if (lblDistancia.Hidden)
                            lblDistancia.Hidden = false;

                        lblDistancia.Text = "Distancia: " + distancia + " ms";
                    });
                }
            }

            private double calculateDistance(SCNVector3 vector1, SCNVector3 vector2)
            {
                double reply = 0;

                var x0 = vector1.X;
                var x1 = vector2.X;
                var y0 = vector1.Y;
                var y1 = vector2.Y;
                var z0 = vector1.Z;
                var z1 = vector2.Z;

                reply = Math.Sqrt(Math.Pow(x1 - x0, 2) + Math.Pow(y1 - y0, 2) + Math.Pow(z1 - z0, 2));

                return reply;
            }

        }

        class Pizarra : UIImageView
        {
            public void DrawLine(CGPoint pointA, CGPoint pointB)
            {
                UIGraphics.BeginImageContext(this.Frame.Size);
                CGContext context = UIGraphics.GetCurrentContext();
                context.SetLineWidth(4);
                UIColor.Blue.SetFill();
                UIColor.Red.SetStroke();
                    
                var currentPath = new CGPath();
                CGPoint[] points = new CGPoint[2];
                points[0] = pointA;
                points[1] = pointB;
                currentPath.AddLines(points);
                currentPath.CloseSubpath();

                context.AddPath(currentPath);
                context.DrawPath(CGPathDrawingMode.Stroke);

                UIImage result = UIGraphics.GetImageFromCurrentImageContext();
                Image = result;

                UIGraphics.EndImageContext();
            }
        }
    }
}