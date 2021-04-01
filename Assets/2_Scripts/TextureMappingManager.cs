using System;
using UnityEngine;
using System.Collections;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using SpatialAwarenessHandler = Microsoft.MixedReality.Toolkit.SpatialAwareness.IMixedRealitySpatialAwarenessObservationHandler<Microsoft.MixedReality.Toolkit.SpatialAwareness.SpatialAwarenessMeshObject>;

public class TextureMappingManager : MonoBehaviour, SpatialAwarenessHandler
{
    public TakePicture takePicture;
    public Material textureMappingMaterial;
    //private bool scanComplete = false;

    private void Awake()
    {

    }

    protected void OnTextureUpdated()
    {
        StartCoroutine(UpdateAllMap());
        Debug.Log("Update des textures");
    }

    protected IEnumerator UpdateAllMap()
    {
        var observer = CoreServices.GetSpatialAwarenessSystemDataProvider<IMixedRealitySpatialAwarenessMeshObserver>();
        foreach (SpatialAwarenessMeshObject meshObject in observer.Meshes.Values)
        {
            GameObject meshGameObject = meshObject.GameObject;
            meshGameObject.GetComponent<ImageTextureMapping>().ApplyTextureMapping(takePicture.worldToCameraMatrixList, takePicture.projectionMatrixList, takePicture.textureArray);
            yield return null;
        }
    }

    void Start()
    {
        var observer = CoreServices.GetSpatialAwarenessSystemDataProvider<IMixedRealitySpatialAwarenessMeshObserver>();
        takePicture.OnTextureUpdated += OnTextureUpdated;
    }

    private void ApplyTextureMapping(GameObject obj)
    {
        var imageTextureMapping = obj.GetComponent<ImageTextureMapping>();
        if (imageTextureMapping == null)
        {
            imageTextureMapping = obj.AddComponent<ImageTextureMapping>();
        }
        imageTextureMapping.ApplyTextureMapping(takePicture.worldToCameraMatrixList, takePicture.projectionMatrixList, takePicture.textureArray);
    }

    private void OnEnable()
    {
        // Register component to listen for Mesh Observation events, typically done in OnEnable()
        CoreServices.SpatialAwarenessSystem.RegisterHandler<SpatialAwarenessHandler>(this);
    }

    private void OnDisable()
    {
        // Unregister component from Mesh Observation events, typically done in OnDisable()
        CoreServices.SpatialAwarenessSystem.UnregisterHandler<SpatialAwarenessHandler>(this);
    }

    public void OnObservationAdded(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
    {
        GameObject meshObject = eventData.SpatialObject.GameObject;
        ApplyTextureMapping(meshObject);
        meshObject.GetComponent<Renderer>().material = textureMappingMaterial;
    }

    public void OnObservationUpdated(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
    {
        ApplyTextureMapping(eventData.SpatialObject.GameObject);
    }

    public void OnObservationRemoved(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData){}
}