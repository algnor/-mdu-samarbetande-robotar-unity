using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using OpenCvSharp;
using TMPro;

public class test2 : MonoBehaviour
{
    [SerializeField]
    ARCameraManager cameraManager = null;
    [SerializeField]
    Material filter;
    [SerializeField]
    RawImage rawImage, outImage;

    [SerializeField]
    TMP_InputField hMin, hMax, sMin, sMax, vMin, vMax, rho, theta, threshold, minLineLength, maxLineGap;
    Texture2D m_Texture, outTexture, FilteredImage;

    Mat img = new Mat();
    Mat sourceMat = new Mat();
    Mat outMat = new Mat();
    RenderTexture LastActive, tmp;

    [SerializeField]
    int frameSkip = 3;
    int frameNr = 0;
    
    void Awake()
    {
        cameraManager.frameReceived += OnCameraFrameReceived;
    }

    void Start() {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
    }
    unsafe void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        frameNr += 1;
        if (!( frameNr % frameSkip == 0) ) {
            return;
        }
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            return;

        var conversionParams = new XRCpuImage.ConversionParams
        {
            // Get the entire image.
            inputRect = new RectInt(0, 0, image.width, image.height),

            // Downsample by 2.
            outputDimensions = new Vector2Int(image.width / 2, image.height / 2),

            // Choose RGBA format.
            outputFormat = TextureFormat.RGB24,

            // Flip across the vertical axis (mirror image).
            transformation = XRCpuImage.Transformation.MirrorX
        };

        // See how many bytes you need to store the final image.
        int size = image.GetConvertedDataSize(conversionParams);

        // Allocate a buffer to store the image.
        var buffer = new Unity.Collections.NativeArray<byte>(size, Unity.Collections.Allocator.Temp);

        // Extract the image data
        image.Convert(conversionParams, new System.IntPtr(buffer.GetUnsafePtr()), buffer.Length);

        // The image was converted to RGBA32 format and written into the provided buffer
        // so you can dispose of the XRCpuImage. You must do this or it will leak resources.
        image.Dispose();

        // At this point, you can process the image, pass it to a computer vision algorithm, etc.
        // In this example, you apply it to a texture to visualize it.

        // You've got the data; let's put it into a texture so you can visualize it.
        if (m_Texture == null)
        {
            m_Texture = new Texture2D(
                conversionParams.outputDimensions.x,
                conversionParams.outputDimensions.y,
                conversionParams.outputFormat,
                false);
        }



        m_Texture.name = "m_texture";
        m_Texture.LoadRawTextureData(buffer);
        m_Texture.Apply();

        if (FilteredImage == null)
        {
            FilteredImage = new Texture2D(m_Texture.width, m_Texture.height);
            outTexture = new Texture2D(m_Texture.width, m_Texture.height);
            tmp = RenderTexture.GetTemporary(m_Texture.width, m_Texture.height, 0);
        }
        // Done with temporary data, so you can dispose it.  
        buffer.Dispose();


        rawImage.texture = m_Texture;

        tmp.name = "tmp";
        filter.SetTexture("_image", m_Texture);

        filter.SetVector("_hue", new Vector4(float.Parse(hMin.text), float.Parse(hMax.text), 0, 0));
        filter.SetVector("_sat", new Vector4(float.Parse(sMin.text), float.Parse(sMax.text), 0, 0));
        filter.SetVector("_val", new Vector4(float.Parse(vMin.text), float.Parse(vMax.text), 0, 0));

        UnityEngine.Graphics.Blit(m_Texture, tmp, filter);

        LastActive = RenderTexture.active;
        LastActive.name = "lastActive";

        RenderTexture.active = tmp;

        FilteredImage.name = "filteredImage";
        FilteredImage.ReadPixels(new UnityEngine.Rect(0, 0, tmp.width, tmp.height), 0, 0);
        FilteredImage.Apply();


        RenderTexture.active = LastActive;
        //RenderTexture.ReleaseTemporary(tmp);

        //outImage.texture = FilteredImage;

        img = OpenCvSharp.Unity.TextureToMat(FilteredImage);
        Mat[] channels;
        Cv2.Split(img, out channels);
        img = channels[2];


        sourceMat = OpenCvSharp.Unity.TextureToMat(FilteredImage);

        Cv2.CvtColor(sourceMat, outMat, ColorConversionCodes.RGB2GRAY);

        OpenCvSharp.Unity.MatToTexture(outMat, outTexture);

        outImage.texture = outTexture;
        
        outTexture.name = "outTexture";

        return;

    }
}