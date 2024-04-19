﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

namespace Slicing
{
    public static class SlicePlane
    {
        public static bool CalculateIntersectionPlane(
            [NotNullWhen(true)] out SlicePlaneCoordinates? sliceCoords,
            [NotNullWhen(true)] out Texture2D? texture,
            Plane plane,
            Model.Model model, IReadOnlyList<Vector3> intersectionPoints,
            Vector3? alternativeStartPoint = null,
            InterpolationType interpolationType = InterpolationType.Nearest)
        {
            sliceCoords = GetSliceCoordinates(plane, model, intersectionPoints);
            if (sliceCoords == null)
            {
                texture = null;
                return false;
            }
            Debug.Log($"Slice Coords: {sliceCoords}");

            texture = CalculateIntersectionPlane(model, sliceCoords, alternativeStartPoint, interpolationType);
            return true;
        }
        
        public static Texture2D CalculateIntersectionPlane(Model.Model model, SlicePlaneCoordinates sliceCoords, Vector3? alternativeStartPoint = null, InterpolationType interpolationType = InterpolationType.Nearest)
        {
            var resultImage = new Texture2D(sliceCoords.Width, sliceCoords.Height);

            var startPoint = alternativeStartPoint ?? sliceCoords.StartPoint;

            //var x = 0;
            for (var x = 0; x < sliceCoords.Width; x++)
            {
                for (var y = 0; y < sliceCoords.Height; y++)
                {
                    // get world position
                    var position = startPoint + sliceCoords.XSteps * x + sliceCoords.YSteps * y;

                    // convert position into index
                    var diff = position - model.BottomFrontLeftCorner;
                    var xStep = Mathf.RoundToInt(diff.x / (model.Size.x / model.XCount));
                    var yStep = Mathf.RoundToInt(diff.y / (model.Size.y / model.YCount));
                    var zStep = Mathf.RoundToInt(diff.z / (model.Size.z / model.ZCount));

                    //Debug.Log($"Before X: {xStep}, Y: {yStep}, Z: {zStep}");

                    // get image at index and then the pixel
                    var pixel = model.GetPixel(xStep, yStep, zStep - 1, interpolationType); // z needs correction
                    resultImage.SetPixel(sliceCoords.Width - x - 1, y, pixel);
                }
            }

            resultImage.Apply();
            return resultImage;
        }
        
        private static SlicePlaneCoordinates? GetSliceCoordinates(Plane plane, Model.Model model, IReadOnlyList<Vector3> intersectionPoints)
        {
            // intersectionPoints are pre-sorted counter-clockwise
            if (intersectionPoints.Count == 3)
            {
                return GetSliceCoordinates3Points(model, intersectionPoints[0], intersectionPoints[1], intersectionPoints[2]);
            }
            else if (intersectionPoints.Count == 4)
            {
                return GetSliceCoordinates4Points(model, intersectionPoints[0], intersectionPoints[1], intersectionPoints[2]);
            }
            else if (intersectionPoints.Count == 6)
            {
                return GetSliceCoordinates6Points(model, intersectionPoints[0], intersectionPoints[1], intersectionPoints[2], intersectionPoints[3], intersectionPoints[4], intersectionPoints[5]);
            }
            else
            {
                Debug.LogError($"Can't create plane with {intersectionPoints.Count} points!");
                return null;
            }

            // get bottom left edge (get left-most point and bottom-most point and check where they meet on the plane)
            var forward = plane.normal;
            var rotation = Quaternion.LookRotation(forward);

            var up = rotation * Vector3.up;
            var down = rotation * Vector3.down;

            // yes, they are swapped!
            var left = rotation * Vector3.right;
            var right = rotation * Vector3.left;

            // to get the left-most point the following algorithm is run:
            // construct a horizontal plane at to bottom-most point
            // for every intersection point, raycast with the down direction until the plane is hit (we get the distance to the bottom)
            // move all points onto the new plane
            // now get the left-most point

            // 1) get start point at the bottom left
            var minPoint = intersectionPoints.Select(p => p.y).Min();
            var lowerPlane = new Plane(Vector3.up, -minPoint);
            var lowerPoints = intersectionPoints.Select(p =>
            {
                var ray = new Ray(p, down);
                lowerPlane.Raycast(ray, out var distance);
                return p + down * distance;
            });

            // we take the first point as base measurement and compare with all other points
            var first = lowerPoints.First();
            lowerPoints = lowerPoints.OrderBy(p => Vector3.Distance(first, p)).ToArray();
            var lowerLeft = lowerPoints.First();
            var lowerRight = lowerPoints.Last();

            // 1.5) and also at the top right to calculate the difference
            var maxPoint = intersectionPoints.Select(p => p.y).Max();
            var upperPlane = new Plane(Vector3.up, -maxPoint);
            var upperPoints = intersectionPoints.Select(p =>
            {
                var ray = new Ray(p, up);
                upperPlane.Raycast(ray, out var distance);
                return p + up * distance;
            });

            //var last = upperPoints.Last();
            //upperPoints = upperPoints.OrderBy(p => Vector3.Distance(last, p)).ToArray();
            var upperLeft = upperPoints.Last();
            var upperRight = upperPoints.First();

            // TODO
            // 2) we get the width and height of the new texture
            // what we do is:
            // for width we only look at X and Z axis and we get the one with the most pixels
            // for height we DON'T look at the Y height, we compare the point of the lower points with the higher points and count pixels

            // we need to convert the world coordinates of the intersection points
            // to int-steps based on the model X/Y/Z-Counts
            Debug.Log($"X: {model.XCount}, Y: {model.YCount}, Z: {model.ZCount}");

            Debug.Log($"Size: {model.Size}, Steps: {model.Steps}");

        }

        private static SlicePlaneCoordinates GetSliceCoordinates3Points(Model.Model model, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            // TODO convert to 4 point
            return null;
        }

        private static SlicePlaneCoordinates GetSliceCoordinates4Points(Model.Model model, Vector3 ul, Vector3 ll, Vector3 lr)
        {
            var diffHeight = ul - ll;
            var diffXZ = lr - ll;

            // this is for calculating steps for height
            var ySteps = Mathf.RoundToInt(diffHeight.y / model.Steps.y);    // Math.Abs is not needed, ySteps is ALWAYS from bottom to top
            var forwardStepsX = Math.Abs(Mathf.RoundToInt(diffHeight.x / model.Steps.x));
            var forwardStepsZ = Math.Abs(Mathf.RoundToInt(diffHeight.z / model.Steps.z));

            // this is for calculating steps for width
            var xSteps = Mathf.RoundToInt(diffXZ.x / model.Steps.x);
            var zSteps = Mathf.RoundToInt(diffXZ.z / model.Steps.z);

            var height = Math.Max(Math.Max(ySteps, forwardStepsX), forwardStepsZ);
            var width = Math.Max(xSteps, zSteps);

            // 3) we get the step size using the edge points and width and height
            var textureStepX = (lr - ll) / width;
            var textureStepY = (ul - ll) / height;

            //Debug.DrawRay(ul, Vector3.up, Color.red, 120);
            //Debug.DrawRay(ll, Vector3.up, Color.green, 120);
            //Debug.DrawRay(lr, Vector3.up, Color.blue, 120);

            return new SlicePlaneCoordinates(width, height, ll, textureStepX, textureStepY);
        }

        private static SlicePlaneCoordinates GetSliceCoordinates6Points(Model.Model model, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Vector3 p5, Vector3 p6)
        {
            // TODO convert to 4 point
            return null;
        }
    }
}