/*
 * Copyright 2017 tarukosu
 * This file contains Original Code and/or Modifications of Original Code
 * licensed under the Apache License, Version 2.0 (the "License");
 * Orignal Code is
 *  Copyright 2017 ywj7931
 *  https://github.com/ywj7931/Hololens_Image_Based_Texture/blob/master/Assets/Scripts/TakePicture.cs
 */
 
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


using System.IO;
using System;
using UnityEngine.XR.WSA.WebCam;
using UnityEngine.XR.WSA.Input;
using UnityEditor;

//Imports to interact with Camera
#if WINDOWS_UWP
using System.Runtime.InteropServices;
using Windows.Media.Devices;
#endif

public class TakePicture : MonoBehaviour
{
    //data copying fronm list to pass to shader
    [System.NonSerialized]
    public Texture2DArray textureArray;

    //the max photos numbers that can be taken, shader doesn't support dynamic array
    public int maxPhotoNum;
    //Locks in the exposure and white balance after first photo.
    public bool lockCameraSettings = true;

    //camera paramaters
    private CameraParameters m_CameraParameters;
    Texture2D m_Texture;
    byte[] bytes;

    //temp data waiting to pass to shader
    [System.NonSerialized]
    public List<Matrix4x4> projectionMatrixList;
    [System.NonSerialized]
    public List<Matrix4x4> worldToCameraMatrixList;


    private bool isCapturingPhoto = false;
    private PhotoCapture photoCaptureObj;
    private int currentPhoto = 0;
    private GestureRecognizer recognizer;
    
#if UNITY_EDITOR
    private const int TEXTURE_WIDTH = 512;
    private const int TEXTURE_HEIGHT = 256;
#else
    private const int TEXTURE_WIDTH = 1024;//512;
    private const int TEXTURE_HEIGHT = 512;//256
#endif

    public event Action OnTextureUpdated;

    private string path;

    #region debug
    public Texture2D[] SampleTexture;
    #endregion

    void Start()
    {
        //init
        projectionMatrixList = new List<Matrix4x4>();
        worldToCameraMatrixList = new List<Matrix4x4>();

        path = Application.persistentDataPath;
        path = Path.Combine(path, "Room");
        Directory.CreateDirectory(path);

        //for debug
#if UNITY_EDITOR
        StartCoroutine(DebugCapture());
#endif

        //init camera
        InitCamera();
    }

    IEnumerator DebugCapture()
    {
        while (true)
        {
            yield return new WaitForSeconds(4);
            OnPhotoCapturedDebug();
        }
    }


    void InitCamera()
    {
        List<Resolution> resolutions = new List<Resolution>(PhotoCapture.SupportedResolutions);

#if !UNITY_EDITOR
        //1280 * 720
        Resolution selectedResolution = resolutions[0];

        //camera params
        m_CameraParameters = new CameraParameters(WebCamMode.PhotoMode)
        {
            cameraResolutionWidth = selectedResolution.width,
            cameraResolutionHeight = selectedResolution.height,
            hologramOpacity = 0.0f,
            pixelFormat = CapturePixelFormat.BGRA32
        };
#endif

        textureArray = new Texture2DArray(TEXTURE_WIDTH, TEXTURE_HEIGHT, maxPhotoNum, TextureFormat.DXT5, false);
        var clearTexture = new Texture2D(TEXTURE_WIDTH, TEXTURE_HEIGHT, TextureFormat.ARGB32, false);

        var resetColorArray = clearTexture.GetPixels();
        for (int i = 0; i < resetColorArray.Length; i++)
        {
            resetColorArray[i] = Color.clear;
        }
        clearTexture.SetPixels(resetColorArray);
        clearTexture.Apply();
        clearTexture.Compress(true);

        Graphics.CopyTexture(clearTexture, 0, 0, textureArray, 0, 0); //Copies the last texture

#if !UNITY_EDITOR
        PhotoCapture.CreateAsync(false, OnCreatedPhotoCaptureObject);
#endif
    }

    void OnCreatedPhotoCaptureObject(PhotoCapture captureObject)
    {
        photoCaptureObj = captureObject;
        photoCaptureObj.StartPhotoModeAsync(m_CameraParameters, OnStartPhotoMode);
    }

    void OnStartPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        //TextManager.Instance.setText("Ready to take photos, say \"Photo\"");
    }

    public void TakePhoto()
    {
        if (isCapturingPhoto)
        {
            return;
        }
        isCapturingPhoto = true;
        photoCaptureObj.TakePhotoAsync(OnPhotoCaptured);
    }

    void OnPhotoCaptured(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        //After the first photo, we want to lock in the current exposure and white balance settings.
        if (lockCameraSettings && currentPhoto == 1)
        {
#if WINDOWS_UWP
        unsafe{
            //This is how you access a COM object.
            VideoDeviceController vdm = (VideoDeviceController)Marshal.GetObjectForIUnknown(photoCaptureObj.GetUnsafePointerToVideoDeviceController());
            //Locks current exposure for all future images
            vdm.ExposureControl.SetAutoAsync(false); //Figureout how to better handle the Async
            //Locks the current WhiteBalance for all future images
            vdm.WhiteBalanceControl.SetPresetAsync(ColorTemperaturePreset.Manual); 
        }
#endif
        }
        //temp to store the matrix
        Matrix4x4 cameraToWorldMatrix;

        photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMatrix);
        Matrix4x4 worldToCameraMatrix = cameraToWorldMatrix.inverse;

        Matrix4x4 projectionMatrix;
        photoCaptureFrame.TryGetProjectionMatrix(out projectionMatrix);

#if UNITY_EDITOR
        projectionMatrix = GetDummyProjectionMatrix();
#endif

        projectionMatrixList.Add(projectionMatrix);
        worldToCameraMatrixList.Add(worldToCameraMatrix);

        m_Texture = new Texture2D(m_CameraParameters.cameraResolutionWidth, m_CameraParameters.cameraResolutionHeight, TextureFormat.RGBA32, false);
        photoCaptureFrame.UploadImageDataToTexture(m_Texture);

        m_Texture.wrapMode = TextureWrapMode.Clamp;

        m_Texture = ResizeTexture(m_Texture, TEXTURE_WIDTH, TEXTURE_HEIGHT);

        photoCaptureFrame.Dispose();

        //save room to png
        bytes = m_Texture.EncodeToPNG();
        //write to LocalState folder
        string imageName = string.Format("Room_{0}.png", currentPhoto + 1);
        string filePath = Path.Combine(path, imageName);
        File.WriteAllBytes(filePath, bytes);

        m_Texture.Compress(true);
        Graphics.CopyTexture(m_Texture, 0, 0, textureArray, currentPhoto + 1, 0);
        textureArray.Apply();

        if (OnTextureUpdated != null)
        {
            OnTextureUpdated();
        }
        currentPhoto++;
        isCapturingPhoto = false;
        Resources.UnloadUnusedAssets();
    }


    void OnPhotoCapturedDebug()
    {
        if (currentPhoto == SampleTexture.Length)
        {
            return;
        }

        Matrix4x4 cameraToWorldMatrix = Matrix4x4.identity;
        cameraToWorldMatrix.m00 = -1f;
        cameraToWorldMatrix.m11 = -1f;
        cameraToWorldMatrix.m22 = -1f;

        if (currentPhoto % SampleTexture.Length == 1)
        {
            cameraToWorldMatrix.m03 = 0.3f;
        }

        Matrix4x4 worldToCameraMatrix = cameraToWorldMatrix.inverse;
        Matrix4x4 projectionMatrix = GetDummyProjectionMatrix();

        projectionMatrixList.Add(projectionMatrix);
        worldToCameraMatrixList.Add(worldToCameraMatrix);

        var texture = SampleTexture[currentPhoto % SampleTexture.Length];
        Graphics.CopyTexture(texture, 0, 0, textureArray, currentPhoto + 1, 0);
        textureArray.Apply();

        if (OnTextureUpdated != null)
        {
            OnTextureUpdated();
        }
        currentPhoto += 1;
        isCapturingPhoto = false;
    }

    //Helper function to resize from top left
    Texture2D ResizeTexture(Texture2D input, int width, int height)
    {
        Color[] pix = input.GetPixels(0, input.height - height, width, height);
        input.Resize(width, height);
        input.SetPixels(pix);
        input.Apply();
        return input;
    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        photoCaptureObj.Dispose();
        photoCaptureObj = null;
    }

    public void StopCamera()
    {
        if (photoCaptureObj != null)
        {
            photoCaptureObj.StopPhotoModeAsync(OnStoppedPhotoMode);
        }
    }

    protected Matrix4x4 GetDummyProjectionMatrix()
    {
        /*
        2.43241 0.00000  0.07031 0.00000
        0.00000 4.31949  0.02752 0.00000
        0.00000 0.00000 -1.00000 0.00000
        0.00000 0.00000 -1.00000 0.00000
        */
        Matrix4x4 projectionMatrix = Matrix4x4.identity;
        projectionMatrix.m00 = 2.43241f;
        projectionMatrix.m02 = 0.07031f;
        projectionMatrix.m11 = 4.31949f;
        projectionMatrix.m12 = 0.02752f;
        projectionMatrix.m22 = -1f;
        projectionMatrix.m32 = -1f;
        projectionMatrix.m33 = 0f;
        return projectionMatrix;
    }

}
