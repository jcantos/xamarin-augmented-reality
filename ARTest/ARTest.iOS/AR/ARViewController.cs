using System;
using System.Drawing;

using CoreFoundation;
using UIKit;
using Foundation;
using ARKit;
using SceneKit;
using System.Collections.Generic;
using System.Linq;

namespace ARTest.iOS.AR
{
    [Register("ARViewController")]
    public class ARViewController : UIViewController
    {
        private readonly ARSCNView sceneView;
        int numberOfTaps = 0;
        int numberMeditions = 0;
        SCNVector3 startPoint;
        SCNVector3 endPoint;

        UILabel lblD1;
        UILabel lblH1;

        public ARViewController()
        {
            this.sceneView = new ARSCNView
            {
                DebugOptions = ARSCNDebugOptions.ShowFeaturePoints,
                Delegate = new SceneViewDelegate()
            };

            lblD1 = new UILabel();
            lblD1.TextColor = UIColor.Black;
            lblD1.BackgroundColor = UIColor.Yellow;
            lblD1.Font = UIFont.FromName("AppleSDGothicNeo-Bold", 16f);

            lblH1 = new UILabel();
            lblH1.TextColor = UIColor.Black;
            lblH1.BackgroundColor = UIColor.Yellow;
            lblH1.Font = UIFont.FromName("AppleSDGothicNeo-Bold", 16f);

            var frameD1 = lblD1.Frame;
            frameD1.X = 25;
            frameD1.Y = 25;
            frameD1.Width = 180;
            frameD1.Height = 25;
            lblD1.Frame = frameD1;

            var frameH1 = lblH1.Frame;
            frameH1.X = 25;
            frameH1.Y = 60;
            frameH1.Width = 180;
            frameH1.Height = 25;
            lblH1.Frame = frameH1;

            this.sceneView.AddSubview(lblD1);
            this.sceneView.AddSubview(lblH1);
            this.View.AddSubview(this.sceneView);
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            this.sceneView.Frame = this.View.Frame;

            var gestureRecognizer = new UITapGestureRecognizer((args) =>
            {
                var point = args.LocationInView(this.sceneView);
                var hitTestResults = this.sceneView.HitTest(point, ARHitTestResultType.FeaturePoint);
                ARHitTestResult hitTest = hitTestResults.FirstOrDefault();
                if (hitTest == null)
                    return;

                //InvokeOnMainThread(() =>
                //{
                //    lblDistance.Text = "Distancia: " + hitTest.Distance;
                //});

                numberOfTaps++;

                if (numberOfTaps == 1)
                {
                    if (numberMeditions == 0)
                        clearScene();

                    startPoint = new SCNVector3(hitTest.WorldTransform.Column3.X,
                        hitTest.WorldTransform.Column3.Y,
                        hitTest.WorldTransform.Column3.Z);
                    addMarker(hitTest);
                }
                else
                {
                    numberOfTaps = 0;
                    numberMeditions++;

                    endPoint = new SCNVector3(hitTest.WorldTransform.Column3.X,
                        hitTest.WorldTransform.Column3.Y,
                        hitTest.WorldTransform.Column3.Z);
                    addMarker(hitTest);
                    addLineBetween(startPoint, endPoint);

                    var distance = calculateDistance(startPoint, endPoint);
                    var middlePoint = calculateCenter(startPoint, endPoint);
                    addDistanceText(distance, middlePoint);

                    if (numberMeditions > 1)
                        numberMeditions = 0;
                }

            });

            this.sceneView.AddGestureRecognizer(gestureRecognizer);
        }

        private void clearScene()
        {
            foreach (var node in sceneView.Scene.RootNode.ChildNodes)
                node.RemoveFromParentNode();

            //lblDistance.Text = "";
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

        private SCNVector3 calculateCenter(SCNVector3 vector1, SCNVector3 vector2)
        {
            var x0 = vector1.X;
            var x1 = vector2.X;
            var y0 = vector1.Y;
            var y1 = vector2.Y;
            var z0 = vector1.Z;
            var z1 = vector2.Z;

            var centerX = (x1 + x0) / 2;
            var centerY = (y1 + y0) / 2;
            var centerZ = (z1 + z0) / 2;

            var center = new SCNVector3(centerX, centerY, centerZ);

            return center;
        }

        private void addMarker(ARHitTestResult hitTestResult)
        {
            var geometry = SCNSphere.Create((nfloat)0.01);
            geometry.FirstMaterial.Diffuse.Contents = UIColor.Red;

            var markerNode = SCNNode.Create();
            markerNode.Geometry = geometry;
            markerNode.Position = new SCNVector3(hitTestResult.WorldTransform.Column3.X,
                hitTestResult.WorldTransform.Column3.Y,
                hitTestResult.WorldTransform.Column3.Z);

            sceneView.Scene.RootNode.AddChildNode(markerNode);
        }

        private void addLineBetween(SCNVector3 start, SCNVector3 end)
        {
            var vertices = new SCNVector3[2];
            vertices[0] = start;
            vertices[1] = end;
            var source = SCNGeometrySource.FromVertices(vertices);

            var indices = new int[2];
            indices[0] = 0;
            indices[1] = 1;
            NSData indexData = NSData.FromArray(indices.SelectMany(v => BitConverter.GetBytes(v)).ToArray());
            var element = SCNGeometryElement.FromData(indexData, SCNGeometryPrimitiveType.Line, 1, sizeof(int));

            var lineGeometry = SCNGeometry.Create(new[] { source }, new[] { element });
            lineGeometry.FirstMaterial.Diffuse.Contents = UIColor.Red;
            var lineNode = SCNNode.Create();
            lineNode.Geometry = lineGeometry;
            sceneView.Scene.RootNode.AddChildNode(lineNode);
        }

        private void addDistanceText(double distance, SCNVector3 point)
        {
            string distanceString = Math.Round((distance * 100), 0) + " cms";
            var textGeometry = SCNText.Create(str: distanceString, extrusionDepth: 1);
            textGeometry.Font = UIFont.SystemFontOfSize(10);
            textGeometry.FirstMaterial.Diffuse.Contents = UIColor.Black;

            var textNode = SCNNode.Create();
            textNode.Geometry = textGeometry;
            textNode.Position = new SCNVector3(point.X, point.Y, point.Z);
            textNode.Scale = new SCNVector3((float)0.001, (float)0.001, (float)0.001);

            // ajustamos la orientación del nodo a la vista de la cámara
            var billboardConstraints = SCNBillboardConstraint.Create();
            textNode.Constraints = new[] { billboardConstraints };

            sceneView.Scene.RootNode.AddChildNode(textNode);

            InvokeOnMainThread(() =>
            {
                double _distance = Math.Round(distance, 2);
                if (numberMeditions == 1)
                    lblD1.Text = "D1: " + _distance + "m";
                else if (numberMeditions == 2)
                    lblH1.Text = "H1: " + _distance + "m";
            });
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            this.sceneView.Session.Run(new ARWorldTrackingConfiguration
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

            this.sceneView.Session.Pause();
        }

        class SceneViewDelegate : ARSCNViewDelegate
        {
            public override void DidAddNode(ISCNSceneRenderer renderer, SCNNode node, ARAnchor anchor)
            {
                if (anchor is ARPlaneAnchor planeAnchor)
                {
                    // Do something with the plane anchor
                }
            }

            public override void DidRemoveNode(ISCNSceneRenderer renderer, SCNNode node, ARAnchor anchor)
            {
                if (anchor is ARPlaneAnchor planeAnchor)
                {
                    // Do something with the plane anchor
                }
            }

            public override void DidUpdateNode(ISCNSceneRenderer renderer, SCNNode node, ARAnchor anchor)
            {
                if (anchor is ARPlaneAnchor planeAnchor)
                {
                    // Do something with the plane anchor
                }
            }
        }
    }
}