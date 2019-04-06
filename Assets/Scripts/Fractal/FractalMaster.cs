﻿using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class FractalMaster : MonoBehaviour
{

    public ComputeShader fractalShader;

    [Range (1, 20)]
    public float fractalPower = 10f;
    public float darkness = 70f;
    public float drawDistance = 200f;

    [Header ("Colour mixing")]
    [Range (0, 1)] public float blackAndWhite;
    [Range (0, 1)] public float redA;
    [Range (0, 1)] public float greenA;
    [Range (0, 1)] public float blueA = 1;
    [Range (0, 1)] public float redB = 1;
    [Range (0, 1)] public float greenB;
    [Range (0, 1)] public float blueB;

    RenderTexture target;
    Camera cam;
    Light directionalLight;

    [Header ("Animation Settings")]
    public float powerIncreaseRate = 0.2f;
    public float oscillationRate = 0.2f;
    public float oscillationRange = 5f;
    private float t = 0;

    private int handleCSMain;

    void Start()
    {
        Application.targetFrameRate = 60;

        if (null == fractalShader)
        {
            Debug.Log("Shader missing.");
            return;
        }
    }
    
    void Init ()
    {
        cam = Camera.current;
        directionalLight = FindObjectOfType<Light> ();
    }

    // Animate properties
    void Update () {
        if (Application.isPlaying)
        {
            if (powerIncreaseRate != 0)
            {
                fractalPower += powerIncreaseRate * Time.deltaTime;
            }

            else
            {
                t = (t + (Time.deltaTime * oscillationRate)) % (2 * Mathf.PI);

                fractalPower = 1f + oscillationRange * (1f + (Mathf.Cos(t + Mathf.PI)));
            }
        }
    }

    void OnRenderImage (RenderTexture source, RenderTexture destination)
    {
        handleCSMain = fractalShader.FindKernel("CSMain");
        Init();
        InitRenderTexture();
        SetParameters();


        int threadGroupsX = Mathf.CeilToInt(cam.pixelWidth / 8.0f);     //CREATING A THREAD FOR EACH PIXEL (/8 AS IT'S *8 IN THE SHADER)
        int threadGroupsY = Mathf.CeilToInt(cam.pixelHeight / 8.0f);
        fractalShader.Dispatch(handleCSMain, threadGroupsX, threadGroupsY, 1);

        Graphics.Blit(target, destination);
    }

    void SetParameters () {
        fractalShader.SetTexture (0, "Destination", target);
        fractalShader.SetFloat ("power", Mathf.Max (fractalPower, 1.01f));
        fractalShader.SetFloat ("darkness", darkness);
        fractalShader.SetFloat ("blackAndWhite", blackAndWhite);
        fractalShader.SetFloat("maxDst", drawDistance);
        fractalShader.SetVector ("colourAMix", new Vector3 (redA, greenA, blueA));
        fractalShader.SetVector ("colourBMix", new Vector3 (redB, greenB, blueB));

        fractalShader.SetMatrix ("_CameraToWorld", cam.cameraToWorldMatrix);
        fractalShader.SetMatrix ("_CameraInverseProjection", cam.projectionMatrix.inverse);
        fractalShader.SetVector ("_LightDirection", directionalLight.transform.forward);

    }

    void InitRenderTexture () {
        if (target == null || target.width != cam.pixelWidth || target.height != cam.pixelHeight)
        {
            if (target != null)
            {
                target.Release ();
            }

            target = new RenderTexture (cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            target.enableRandomWrite = true;
            target.Create ();
        }
    }
}