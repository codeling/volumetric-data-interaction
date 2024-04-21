﻿#nullable enable

using Constants;
using EzySlice;
using Model;
using System.Linq;
using UnityEngine;

namespace Slicing
{
    /// <summary>
    /// https://github.com/LandVr/SliceMeshes
    /// </summary>
    public class Slicer : MonoBehaviour
    {
        [SerializeField]
        private CutQuad cutQuadPrefab = null!;
        
        [SerializeField]
        private GameObject temporaryCuttingPlane = null!;

        [SerializeField]
        private GameObject sectionQuad = null!;

        [SerializeField]
        private GameObject cuttingPlane = null!;
        
        [SerializeField]
        private Material materialTemporarySlice = null!;
        
        [SerializeField]
        private Material materialWhite = null!;
        
        [SerializeField]
        private Material materialBlack = null!;
        
        [SerializeField]
        private Shader materialShader = null!;
        
        private bool _isTouched;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(Tags.Model))
            {
                return;
            }

            _isTouched = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(Tags.Model))
            {
                return;
            }

            _isTouched = false;
        }

        public void Slice()
        {
            if (!_isTouched)
            {
                return;
            }
            
            Debug.Log("Slicing");

            var cachedTransform = transform;
            var model = ModelManager.Instance.CurrentModel;
            var modelGo = model.gameObject;
            var sectionQuadTransform = sectionQuad.transform;
            
            var slicedObject = modelGo.Slice(cachedTransform.position, cachedTransform.forward);
            if (slicedObject == null)
            {
                Debug.LogError("Nothing sliced");
                return;
            }
            AudioManager.Instance.PlayCameraSound();

            var intersectionPoints = SlicePlane.GetIntersectionPoints(out var plane, model, sectionQuadTransform.position, sectionQuadTransform.rotation);

            if (!SlicePlane.CalculateIntersectionPlane(out _, out var texture, plane, model, intersectionPoints))
            {
                Debug.LogWarning("Intersection image can't be calculated!");
                return;
            }

            var transparentMaterial = MaterialTools.CreateTransparentMaterial();
            transparentMaterial.name = "SliceMaterial";
            transparentMaterial.mainTexture = texture;
            //var sliceMaterial = MaterialTools.GetMaterialOrientation(transparentMaterial, model, sliceCoords.StartPoint);

            Debug.Log($"Sliced gameobject \"{model.name}\"");
            var lowerHull = slicedObject.CreateUpperHull(modelGo, materialBlack);
            model.UpdateModel(lowerHull.GetComponent<MeshFilter>().mesh, gameObject);
            Destroy(lowerHull);
            SetTemporaryCuttingPlaneActive(true);

            intersectionPoints = intersectionPoints.Select(p => model.transform.TransformPoint(p)).ToArray();

            var mesh = SlicePlane.CreateIntersectingMesh(intersectionPoints);
            if (mesh == null)
            {
                Debug.LogWarning($"Cannot create mesh! Number of points: {intersectionPoints.Length}");
                return;
            }

            var quad = Instantiate(cutQuadPrefab, model.transform, true);
            quad.name = "cut";
            quad.Mesh = mesh;
            quad.Material = transparentMaterial;
        }
        
        public void SetTemporaryCuttingPlaneActive(bool active)
        {
            temporaryCuttingPlane.SetActive(active);

            if (active)
            {
                ModelManager.Instance.CurrentModel.SetCuttingPlane(temporaryCuttingPlane);
            }

            ModelManager.Instance.CurrentModel.SetCuttingPlaneActive(active);

            if (active)
            {
                ModelManager.Instance.CurrentModel.SetModelMaterial(materialTemporarySlice, materialShader);
            }
            else
            {
                ModelManager.Instance.CurrentModel.SetModelMaterial(materialWhite);
            }
        }
    }
}