using UnityEngine;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;

public class MiniatureRoom : MonoBehaviour {
    public Dictionary<string, GameObject> MiniatureMeshDictionary = new Dictionary<string, GameObject>();
    public TakePicture takePicture;

    void Start() {
        takePicture.OnTextureUpdated += OnTextureUpdated;
    }

    protected void OnTextureUpdated()
    {
        GenerateMesh();
    }

    public void GenerateMesh()
    {
        var observer = CoreServices.GetSpatialAwarenessSystemDataProvider<IMixedRealitySpatialAwarenessMeshObserver>();

        foreach (SpatialAwarenessMeshObject meshElement in observer.Meshes.Values)
        {
            GameObject meshElementGO = meshElement.GameObject;
            var name = meshElementGO.name;
            if (MiniatureMeshDictionary.ContainsKey(name))
            {
                var meshObject = MiniatureMeshDictionary[name];
                meshObject.GetComponent<Renderer>().material = meshElementGO.gameObject.GetComponent<Renderer>().material;
            }
            else
            {
                var meshObject = Instantiate(meshElementGO.gameObject, transform);
                meshObject.transform.localScale = 0.1f * Vector3.one;
                MiniatureMeshDictionary.Add(name, meshObject);
            }
        }
    }
}