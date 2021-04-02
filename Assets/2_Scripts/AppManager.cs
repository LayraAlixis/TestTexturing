using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit;

public class AppManager : MonoBehaviour, IMixedRealityPointerHandler 
{
    public enum AppStates
    {
        Scanning, MappingTexture
    };

    public TakePicture takePicture;

    public AppStates State { get; protected set; }
    public MiniatureRoom MiniatureRoom;

    public TextureMappingManager textureMappingManager;

    void Start() 
    {
        State = AppStates.Scanning;
        //StartCoroutine(DisplayScanningMessage());
    }

    void OnEnable()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        MixedRealityToolkit.InputSystem.Register(gameObject);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    private void OnDisable()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        if (MixedRealityToolkit.InputSystem != null)
#pragma warning restore CS0618 // Type or member is obsolete
        {
#pragma warning disable CS0618 // Type or member is obsolete
            MixedRealityToolkit.InputSystem.Unregister(gameObject);
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
    
    public void OnPointerDown(MixedRealityPointerEventData eventData){ }

    public void OnPointerDragged(MixedRealityPointerEventData eventData){ }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        if (eventData.InputSource.SourceType == InputSourceType.Hand)
        {
            var result = eventData.Pointer.Result;
            Debug.Log(result);
            if(result == null || result.CurrentPointerTarget.layer == 31)
            {
                switch (State)
                {
                    case AppStates.Scanning:
                        textureMappingManager.ApplyTextureMappingReload();
                        break;
                    case AppStates.MappingTexture:
                        Debug.Log("Take Photo");
                        takePicture.TakePhoto();
                        break;
                }
            }
        }
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData){}

    public void StartMappingTexture()
    {
        if (State == AppStates.Scanning)
        {
            State = AppStates.MappingTexture;
            CoreServices.SpatialAwarenessSystem.SuspendObservers();
            MiniatureRoom.GenerateMesh();
        }
    }

    public void ChangeState(GameObject myChangingButton)
    {
        StartMappingTexture();
        Destroy(myChangingButton);
    }
}