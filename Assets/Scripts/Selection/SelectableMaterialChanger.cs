using UnityEngine;
using UnityEngine.Serialization;

namespace Selection
{
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(MeshRenderer))]
    public class SelectableMaterialChanger : MonoBehaviour
    {
        [FormerlySerializedAs("greenMaterial")]
        [SerializeField]
        private Material selectedMaterial;
        
        [SerializeField]
        private Material highlightedMaterial;
        
        private Selectable _selectable;
        private MeshRenderer _meshRenderer;
        private Material _defaultMaterial;
        
        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _defaultMaterial = _meshRenderer.material;
        }

        private void OnEnable()
        {
            _selectable.HighlightChanged += OnHighlightChanged;
            _selectable.SelectChanged += OnSelectChanged;
        }

        private void OnDisable()
        {
            _selectable.HighlightChanged -= OnHighlightChanged;
            _selectable.SelectChanged -= OnSelectChanged;
        }
        
        private void OnHighlightChanged(bool isHighlighted) => SetMaterial(isHighlighted ? highlightedMaterial : _defaultMaterial);

        private void OnSelectChanged(bool isSelected) => SetMaterial(isSelected ? selectedMaterial : _defaultMaterial);

        private void SetMaterial(Material mat)
        {
            _meshRenderer.material = mat;
            _meshRenderer.material.mainTexture = _defaultMaterial.mainTexture;
            _meshRenderer.material.mainTextureScale = _defaultMaterial.mainTextureScale;
        }
    }
}