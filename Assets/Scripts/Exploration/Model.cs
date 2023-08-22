﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Constants;
using Helper;
using UnityEngine;

namespace Exploration
{
    /// <summary>
    /// Add scs.rsp to be able to use Bitmaps in Unity
    /// https://forum.unity.com/threads/using-bitmaps-in-unity.899168/
    /// </summary>
    public class Model : MonoBehaviour
    {
        [SerializeField]
        private string stackPath = ConfigurationConstants.X_STACK_PATH_LOW_RES;

        private SlicePlaneFactory _slicePlaneFactory;

        private Collider _collider;

        private BoxCollider _boxCollider;
        
        private const float CropThreshold = 0.1f;

        public Texture2D[] OriginalBitmap { get; private set; }
        
        public int XCount { get; private set; }

        public int YCount { get; private set; }

        public int ZCount { get; private set; }

        private void Awake()
        {
            _slicePlaneFactory = FindObjectOfType<SlicePlaneFactory>();
            _collider = GetComponent<Collider>();
            _boxCollider = GetComponent<BoxCollider>();
            
            OriginalBitmap = InitModel(stackPath);

            XCount = OriginalBitmap.Length;
            YCount = OriginalBitmap.Length > 0 ? OriginalBitmap[0].height : 0;
            ZCount = OriginalBitmap.Length > 0 ? OriginalBitmap[0].width : 0;
        }

        private static Texture2D[] InitModel(string path)
        {
            if (!Directory.Exists(path))
            {
                return Array.Empty<Texture2D>();
            }
            var files = Directory.GetFiles(path);
            var model3D = new Texture2D[files.Length];

            for (var i = 0; i < files.Length; i++)
            {
                var imagePath = Path.Combine(path, files[i]);
                model3D[i] = FileLoader.LoadImage(imagePath);
            }

            return model3D;
        }

        public Vector3 GetCountVector() => new Vector3(XCount, YCount, ZCount);

        public SlicePlane GetIntersectionAndTexture()
        {
            var sectionQuadFull = GameObject.Find(StringConstants.SectionQuad).transform.GetChild(0); // due to slicing the main plane might be incomplete, a full version is needed for intersection calculation
            var modelIntersection = new ModelIntersection(this, _collider, _boxCollider, sectionQuadFull.gameObject, sectionQuadFull.GetComponent<MeshFilter>());
            var intersectionPoints = modelIntersection.GetNormalisedIntersectionPosition();

            var validIntersectionPoints = CalculateValidIntersectionPoints(intersectionPoints);
            var slicePlane = GetIntersectionPlane(validIntersectionPoints.ToList());

            //var fileLocation = FileSaver.SaveBitmapPng(sliceCalculation);
            //var sliceTexture = LoadTexture(fileLocation);
            return slicePlane;
        }

        //public static Texture2D LoadTexture(string fileLocation) => FileLoader.LoadImage($"{fileLocation}.png");
    
        public bool IsXEdgeVector(Vector3 point) => point.x == 0 || (point.x + 1) >= XCount;

        public bool IsZEdgeVector(Vector3 point) =>  point.z == 0 || (point.z + 1) >= ZCount;

        public bool IsYEdgeVector(Vector3 point) => point.y == 0 || (point.y + 1) >= YCount;
        
        private IEnumerable<Vector3> CalculateValidIntersectionPoints(IEnumerable<Vector3> intersectionPoints)
        {
            var croppedIntersectionPoints = intersectionPoints
                .Select(p => ValueCropper.ApplyThresholdCrop(p, GetCountVector(), CropThreshold))
                .ToList();
            
            if (croppedIntersectionPoints.Count < 3)
            {
                throw new Exception("Cannot calculate a cutting plane with fewer than 3 coordinates");
            }

            return croppedIntersectionPoints;
        }

        private SlicePlane GetIntersectionPlane(IReadOnlyList<Vector3> intersectionPoints)
        {
            var slicePlane = _slicePlaneFactory.Create(this, intersectionPoints);
            slicePlane.ActivateCalculationSound();

            return slicePlane;
        }
    }
}