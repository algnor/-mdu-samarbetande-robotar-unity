using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;
using OpenCvSharp;
using TMPro;

public class test : MonoBehaviour
{
    [SerializeField]
    private int cameraIndex = 1;
    [SerializeField]
    TMP_Dropdown cameraDropdown;
    [SerializeField]
    Material filter;
    [SerializeField]
    GameObject original, output;
    [SerializeField]
    TMP_InputField hMin, hMax, sMin, sMax, vMin, vMax, rho, theta, threshold, minLineLength, maxLineGap;
    WebCamTexture _webCamTexture;
    Texture2D FilteredImage;
    Texture2D OutputImage;
    RenderTexture LastActive;
    
    Mat img = new Mat();
    Mat outImg = new Mat();
    // Start is called before the first frame update    

    void setCameraIndex(int index) {
        _webCamTexture.Stop();
        _webCamTexture = new WebCamTexture(WebCamTexture.devices[index].name);
        _webCamTexture.Play();
        FilteredImage = new Texture2D (_webCamTexture.width, _webCamTexture.height);
        OutputImage = new Texture2D (_webCamTexture.width, _webCamTexture.height);
    }
    void Start()
    {
        cameraDropdown.options.Clear();
        cameraDropdown.onValueChanged.AddListener(delegate {setCameraIndex(cameraDropdown.value);});

        WebCamDevice[] devices = WebCamTexture.devices;
        foreach (WebCamDevice camera in devices) {
            Debug.Log(camera.name);
            TMP_Dropdown.OptionData newOption = new TMP_Dropdown.OptionData();
            newOption.text = camera.name;
            cameraDropdown.options.Add(newOption);
        }

        _webCamTexture = new WebCamTexture(devices[cameraIndex].name);

        _webCamTexture.requestedFPS = 60;
        _webCamTexture.Play();  
        _webCamTexture.name = "webcamTexture";
    FilteredImage = new Texture2D (_webCamTexture.width, _webCamTexture.height);
    OutputImage = new Texture2D (_webCamTexture.width, _webCamTexture.height);
    }

    // Update is called once per frame
    void Update()
    {
        //img = OpenCvSharp.Unity.TextureToMat(_webCamTexture);
        //Cv2.CvtColor(img, outImg, ColorConversionCodes.RGB2GRAY);

        RenderTexture tmp = RenderTexture.GetTemporary(_webCamTexture.width, _webCamTexture.height, 0);
        tmp.name = "tmp";
        filter.SetTexture("_image", _webCamTexture);

        filter.SetVector("_hue", new Vector4(float.Parse(hMin.text), float.Parse(hMax.text), 0, 0));
        filter.SetVector("_sat", new Vector4(float.Parse(sMin.text), float.Parse(sMax.text), 0, 0));
        filter.SetVector("_val", new Vector4(float.Parse(vMin.text), float.Parse(vMax.text), 0, 0));

        UnityEngine.Graphics.Blit(_webCamTexture, tmp, filter);

        LastActive = RenderTexture.active;
        LastActive.name = "lastActive";

        RenderTexture.active = tmp;

        FilteredImage.name = "filteredImage";
        FilteredImage.ReadPixels (new UnityEngine.Rect (0, 0, tmp.width, tmp.height), 0, 0);
        FilteredImage.Apply();


        RenderTexture.active = LastActive;
        RenderTexture.ReleaseTemporary (tmp);

        original.GetComponent<Renderer>().material.mainTexture = _webCamTexture;

        img = OpenCvSharp.Unity.TextureToMat(FilteredImage);
        Mat[] channels;
        Cv2.Split(img, out channels);
        img = channels[2];

        //Cv2.Line(img, 0, 0, img.Width / 2, img.Height / 2, new Scalar(255));

        //Cv2.Canny(img, img, float.Parse(canny1.text), float.Parse(canny2.text));

        LineSegmentPoint[] lines;

        lines = Cv2.HoughLinesP(img, float.Parse(rho.text), float.Parse(theta.text), int.Parse(threshold.text), float.Parse(minLineLength.text), float.Parse(maxLineGap.text));
        Debug.Log(lines.Length);
        /*
        foreach (LineSegmentPoint line in lines)
        {
            Cv2.Line(img, line.P1.X, line.P1.Y, line.P2.X, line.P2.Y, new Scalar(255));
        }
        */


        OpenCvSharp.Unity.MatToTexture(img, FilteredImage);
        output.GetComponent<Renderer>().material.mainTexture = FilteredImage;

        return;
    }
}
