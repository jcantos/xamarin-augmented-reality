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

        UILabel lblDistancia;
        UILabel lblDistanciaRealTime;

        UIButton btnCalcular;
        UIImageView screenshot;
        int numTaps = 0;
        int numberMeditions = 0;
        CGPoint pointA;
        CGPoint pointB;
        Pizarra pizarra;

        UILabel lblH1;
        UILabel lblD1;
        UILabel lblD2;
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

            lblDistancia = new UILabel();
            lblDistancia.TextColor = UIColor.Black;
            lblDistancia.BackgroundColor = UIColor.Yellow;
            lblDistancia.Font = UIFont.FromName("AppleSDGothicNeo-Bold", 16f);
            lblDistancia.Hidden = true;

            lblDistanciaRealTime = new UILabel();
            lblDistanciaRealTime.TextColor = UIColor.Black;
            lblDistanciaRealTime.BackgroundColor = UIColor.Yellow;
            lblDistanciaRealTime.Font = UIFont.FromName("AppleSDGothicNeo-Bold", 16f);
            lblDistanciaRealTime.Hidden = true;

            lblH1 = new UILabel();
            lblH1.TextColor = UIColor.Black;
            lblH1.BackgroundColor = UIColor.Yellow;
            lblH1.Font = UIFont.FromName("AppleSDGothicNeo-Bold", 16f);
            lblH1.Hidden = true;

            lblD1 = new UILabel();
            lblD1.TextColor = UIColor.Black;
            lblD1.BackgroundColor = UIColor.Yellow;
            lblD1.Font = UIFont.FromName("AppleSDGothicNeo-Bold", 16f);
            lblD1.Hidden = true;

            lblD2 = new UILabel();
            lblD2.TextColor = UIColor.Black;
            lblD2.BackgroundColor = UIColor.Yellow;
            lblD2.Font = UIFont.FromName("AppleSDGothicNeo-Bold", 16f);
            lblD2.Hidden = true;

            var frameBtn = btnCalcular.Frame;
            frameBtn.X = 250;
            frameBtn.Y = 25;
            frameBtn.Width = 100;
            frameBtn.Height = 48;
            btnCalcular.Frame = frameBtn;

            var frameDistancia = lblDistancia.Frame;
            frameDistancia.X = 25;
            frameDistancia.Y = 25;
            frameDistancia.Width = 180;
            frameDistancia.Height = 25;
            lblDistancia.Frame = frameDistancia;

            var frameDistanciaRealTime = lblDistanciaRealTime.Frame;
            frameDistanciaRealTime.X = 25;
            frameDistanciaRealTime.Y = 25;
            frameDistanciaRealTime.Width = 180;
            frameDistanciaRealTime.Height = 25;
            lblDistanciaRealTime.Frame = frameDistanciaRealTime;

            var frameH1 = lblH1.Frame;
            frameH1.X = 25;
            frameH1.Y = 60;
            frameH1.Width = 180;
            frameH1.Height = 25;
            lblH1.Frame = frameH1;

            var frameD1 = lblD1.Frame;
            frameD1.X = 25;
            frameD1.Y = 95;
            frameD1.Width = 180;
            frameD1.Height = 25;
            lblD1.Frame = frameD1;

            var frameD2 = lblD2.Frame;
            frameD2.X = 25;
            frameD2.Y = 130;
            frameD2.Width = 180;
            frameD2.Height = 25;
            lblD2.Frame = frameD2;

            this.sceneView = new ARSCNView
            {
                DebugOptions = ARSCNDebugOptions.ShowFeaturePoints
            };
            sceneViewDelegate = new SceneViewDelegate(sceneView, lblDistanciaRealTime, medidas);
            this.sceneView.Delegate = sceneViewDelegate;

            this.screenshot.AddSubview(btnCalcular);
            this.screenshot.AddSubview(lblDistancia);
            this.screenshot.AddSubview(lblH1);
            this.screenshot.AddSubview(lblD1);
            this.screenshot.AddSubview(lblD2);
            this.sceneView.AddSubview(lblDistanciaRealTime);
            this.View.AddSubview(this.screenshot);
            this.View.AddSubview(this.pizarra);
            this.View.AddSubview(this.sceneView);
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            this.sceneView.Frame = this.View.Frame;
            this.screenshot.Frame = this.View.Frame;
            this.pizarra.Frame = this.View.Frame;

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

                //InvokeOnMainThread(() =>
                //{
                //    double distancia = Math.Round(hitTest.Distance, 2);
                //    lblDistanciaRealTime.Hidden = false;
                //    lblDistanciaRealTime.Text = "Distancia: " + distancia + " ms";
                //    medidas.DistanciaAlArbol = hitTest.Distance;

                //    //var image = sceneView.Snapshot();
                //    //screenshot.Image = image;
                //    //screenshot.Hidden = false;
                //    //pizarra.Hidden = false;
                //    //sceneView.Hidden = true;
                //});
            });

            var tapPizarra = new UITapGestureRecognizer((args) =>
            {
                numTaps++;

                if (numTaps == 1)
                {
                    pointA = args.LocationInView(args.View);

                    if (numberMeditions == 0)
                        clearScene();
                }
                else if (numTaps == 2)
                {
                    pointB = args.LocationInView(args.View);
                    numTaps = 0;
                    numberMeditions++;

                    pizarra.DrawLine(pointA, pointB);

                    var distance = calculateDistance(pointA, pointB, medidas.DistanciaAlArbol);
                    addDistanceText(distance);

                    if (numberMeditions > 2)
                        numberMeditions = 0;
                }
            });

            this.pizarra.AddGestureRecognizer(tapPizarra);
            this.sceneView.AddGestureRecognizer(tapSceneView);
        }

        private void clearScene()
        {
            foreach (var node in sceneView.Scene.RootNode.ChildNodes)
                node.RemoveFromParentNode();

            if (sceneViewDelegate != null)
                sceneViewDelegate.MarkerNode = null;

            InvokeOnMainThread(() =>
            {
                btnCalcular.Hidden = true;
                lblH1.Hidden = true;
                lblD1.Hidden = true;
                lblD2.Hidden = true;
            });
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
                    lblH1.Text = "H1: " + distanceString;
                    lblH1.Hidden = false;
                }
                else if (numberMeditions == 2)
                {
                    medidas.CopaD1 = distance;
                    lblD1.Text = "D1: " + distanceString;
                    lblD1.Hidden = false;
                }
                else if (numberMeditions == 3)
                {
                    medidas.CopaD2 = distance;
                    lblD2.Text = "D2: " + distanceString;
                    lblD2.Hidden = false;
                    btnCalcular.Hidden = false;
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