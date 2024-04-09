﻿#nullable enable

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Model
{
    public class ModelIntersection
    {
        private readonly Model _model;
        private readonly BoxCollider _modelBoxCollider;
        private readonly Vector3 _slicerPosition;

        private readonly IEnumerable<Vector3> _planeMeshVertices;

        public ModelIntersection(Model model, BoxCollider modelBoxCollider, Vector3 slicerPosition, Quaternion slicerRotation, Matrix4x4 slicerLocalToWorld, Mesh mesh)
        {
            _model = model;
            _modelBoxCollider = modelBoxCollider;
            _slicerPosition = slicerPosition;
            _planeMeshVertices = mesh.vertices.Select(v => slicerLocalToWorld.MultiplyPoint(v));
        }

        public ModelIntersection(Model model, BoxCollider modelBoxCollider, Vector3 slicerPosition, Quaternion slicerRotation, Matrix4x4 slicerLocalToWorld, MeshFilter planeMeshFilter)
            : this(model, modelBoxCollider, slicerPosition, slicerRotation, slicerLocalToWorld, planeMeshFilter.sharedMesh) { }

        public IEnumerable<Vector3> GetNormalisedIntersectionPosition()
        {
            var intersectionPoints = GetIntersectionPoints();
            var halfColliderSize = _modelBoxCollider.size / 2;

            var normalisedPositions = intersectionPoints
                .Select(p => _model.transform.worldToLocalMatrix.MultiplyPoint(p))
                .Select(newPosition => GetNormalisedPosition(newPosition, halfColliderSize));

            return CalculatePositionWithinModel(normalisedPositions, _modelBoxCollider.size);
        }
        
        /// <summary>
        /// https://catlikecoding.com/unity/tutorials/procedural-meshes/creating-a-mesh/
        /// </summary>
        public Mesh CreateIntersectingMesh()
        {
            var originalIntersectionPoints = GetIntersectionPoints();
            var intersectionPoints = GetBoundaryIntersections(originalIntersectionPoints.ToList());

            return new Mesh
            {
                vertices = intersectionPoints.ToArray(),
                triangles = new int[] { 0, 2, 1, 1, 2, 3},
                normals = new Vector3[] { Vector3.back, Vector3.back, Vector3.back , Vector3.back },
                uv = new Vector2[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one }
            };
        }

        private IEnumerable<Vector3> GetIntersectionPoints()
        {
            foreach (var planePoint in _planeMeshVertices)
            {
                var isTouching = false;
                var touchPoint = planePoint;

                // slowly move to center and check if we touch the model
                while (!isTouching && touchPoint != _slicerPosition)
                {
                    touchPoint = Vector3.MoveTowards(touchPoint, _slicerPosition, 0.005f);

                    var hitColliders = Physics.OverlapBox(touchPoint, new Vector3());
                    isTouching = hitColliders.FirstOrDefault(c => c.name == _modelBoxCollider.name);
                    //if (isTouching)
                    //{
                    //    CreateDebugPrimitive(touchPoint, black);
                    //}
                }

                yield return touchPoint;
            }
        }
              
        private IEnumerable<Vector3> CalculatePositionWithinModel(IEnumerable<Vector3> normalisedContacts, Vector3 size)
        {
            foreach (var contact in normalisedContacts)
            {
                var xRelativePosition = (contact.z / size.z) * _model.XCount;
                var yRelativePosition = (contact.y / size.y) * _model.YCount;
                var zRelativePosition = (contact.x / size.x) * _model.ZCount;

                yield return new Vector3(
                    Mathf.Round(xRelativePosition),
                    Mathf.Round(yRelativePosition),
                    Mathf.Round(zRelativePosition));
            }
        }

        private IEnumerable<Vector3> GetBoundaryIntersections(IReadOnlyList<Vector3> intersectionPoints)
        {
            var p1 = SetBoundsPoint(intersectionPoints[0], _modelBoxCollider);
            var p2 = SetBoundsPoint(intersectionPoints[1], _modelBoxCollider);
            var p3 = SetBoundsPoint(intersectionPoints[2], _modelBoxCollider);
            var p4 = SetBoundsPoint(intersectionPoints[3], _modelBoxCollider);

            // vertically
            var v1 = GetMostOuterPointOnBound(p1, p3);
            var v2 = GetMostOuterPointOnBound(p2, p4);
            var v3 = GetMostOuterPointOnBound(p3, p1);
            var v4 = GetMostOuterPointOnBound(p4, p2);

            //horizontally
            var h1 = GetMostOuterPointOnBound(v1, v2);
            var h2 = GetMostOuterPointOnBound(v2, v1);
            var h3 = GetMostOuterPointOnBound(v3, v4);
            var h4 = GetMostOuterPointOnBound(v4, v3);
            
            return new Vector3[] { h1, h2, h3, h4 };
        }

        /// <summary>
        /// Position outside point outside of collider, use two points to create line
        /// move outside point towards original point until collision with collider to find outside border
        /// Beforehand, it was tried to work with ray casting, which was not reliable
        /// See commit f0222339 for obsolete code
        /// </summary>
        private Vector3 GetMostOuterPointOnBound(Vector3 point, Vector3 referencePoint)
        {
            const float threshold = 0.01f;
            const int maxIterations = 10000;
            
            var direction = point - referencePoint;
            var outsidePoint = point + direction * 20;
            var i = 0;
            var distance = 100f;
            while (distance > threshold && i < maxIterations)
            {
                outsidePoint = Vector3.MoveTowards(outsidePoint, point, threshold);
                distance = Vector3.Distance(outsidePoint, _modelBoxCollider.ClosestPoint(outsidePoint));
                i++;
            }

            var result = i == maxIterations ? point : outsidePoint;
            return SetBoundsPoint(result, _modelBoxCollider);
        }

        private static Vector3 GetNormalisedPosition(Vector3 relativePosition, Vector3 minPosition) =>
            relativePosition + minPosition;
        
        private static Vector3 SetBoundsPoint(Vector3 point, BoxCollider collider)
        {
            const float threshold = 0.1f;
            
            var boundsPoint = collider.ClosestPointOnBounds(point);
            var distance = Vector3.Distance(point, boundsPoint);
            return distance > threshold ? point : boundsPoint;
        }
    }
}