#nullable enable

using UnityEngine;

public class MappingAnchor : MonoBehaviour
{
    [SerializeField]
    private Transform tracker = null!;

    private Transform? _model;
    
    private Transform? _parent;
    
    private Vector3 _positionOffset;
    
    private void Update()
    {
        // tracker is null if not mapping
        if (_model == null)
        {
            return;
        }

        var mappingTransform = transform;
        mappingTransform.position = tracker.position + _positionOffset;
        mappingTransform.rotation = tracker.rotation;

        var mappingPosition = mappingTransform.position;
        Debug.DrawRay(mappingPosition, mappingTransform.up, Color.green);
        Debug.DrawRay(mappingPosition, mappingTransform.forward, Color.blue);
        Debug.DrawRay(mappingPosition, mappingTransform.right, Color.red);
    }
    
    public void StartMapping(Transform model)
    {
        Debug.Log("Started Mapping");
        _model = model;
        _parent = model.parent;
        
        var modelPosition = model.position;
        _positionOffset = modelPosition - tracker.position;

        var mappingTransform = transform;
        mappingTransform.position = modelPosition;
        mappingTransform.rotation = tracker.rotation;
        model.SetParent(mappingTransform);
    }

    public void StopMapping()
    {
        if (_model == null)
        {
            return;
        }
        
        Debug.Log("Stopped Mapping");
        _model.SetParent(_parent);
        _model = null;
        _parent = null;
    }
}