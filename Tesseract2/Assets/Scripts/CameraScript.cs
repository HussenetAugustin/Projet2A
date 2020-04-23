using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OpenCvSharp.Demo;
using OpenCvSharp;
using static OpenCvSharp.Unity;
using TMPro;


public class CameraScript : MonoBehaviour
{
    public RawImage background;
    public TesseractDemoScript tesseract;


    private bool camAvailable;
    private bool coroutineEnd;
    private WebCamTexture backCam;
    private PaperScanner scanner = new PaperScanner();
    private Point[] pts;
    private Mat tt;
    private Scalar color;
    private Texture2D t;
    private Texture2D test;
    private bool Stop;
    private bool Stop2;

    // Start is called before the first frame update
    void Start()
    {
        Stop = false;
        Stop2 = false;
        coroutineEnd = true;
        color = new Scalar(255, 0, 0);
        pts = new Point[4];
        tesseract.displayText.text = "";
        tesseract.outputImage = background;


        WebCamDevice[] devices = WebCamTexture.devices;

        if(devices.Length == 0)
        {
            Debug.Log("No Camera detected");
            camAvailable = false;
            return;
        }

        for (int i = 0; i<devices.Length; i++)
        {
#if UNITY_EDITOR
            backCam = new WebCamTexture(devices[i].name, Screen.width, Screen.height);
#elif UNITY_ANDROID
            if(!devices[i].isFrontFacing)
            {
                backCam = new WebCamTexture(devices[i].name, Screen.width, Screen.height);
            }
#endif
        }

        if (backCam == null)
        {
            Debug.Log("Unable to fin a back Camera");
            return;
        }

        backCam.requestedFPS = 60;
        backCam.Play();
        background.texture = backCam;
        background.rectTransform.sizeDelta = new Vector2(backCam.width, backCam.height);

        t = new Texture2D(background.texture.width, background.texture.height);
        test = new Texture2D(background.texture.width, background.texture.height);
        

        camAvailable = true;

    }

    // Update is called once per frame
    void Update()
    {
        if (!camAvailable)
        {
            return;
        }
        if (!Stop)
        {
            StartCoroutine(contour());
        }
        else if (!Stop2)
        {
            StartCoroutine(contour());
            StartCoroutine(StartB());
        }


    }


    // Use this for initialization
    public Point[] Process(Texture2D name)
    {
        Texture2D inputTexture = (Texture2D)name;

        // first of all, we set up scan parameters
        // 
        // scanner.Settings has more values than we use
        // (like Settings.Decolorization that defines
        // whether b&w filter should be applied), but
        // default values are quite fine and some of
        // them are by default in "smart" mode that
        // uses heuristic to find best choice. so,
        // we change only those that matter for us
        scanner.Settings.NoiseReduction = 0.7;                                          // real-world images are quite noisy, this value proved to be reasonable
        scanner.Settings.EdgesTight = 0.9;                                              // higher value cuts off "noise" as well, this time smaller and weaker edges
        scanner.Settings.ExpectedArea = 0.2;                                            // we expect document to be at least 20% of the total image area
        scanner.Settings.GrayMode = PaperScanner.ScannerSettings.ColorMode.Grayscale;   // color -> grayscale conversion mode

        // process input with PaperScanner
        Mat result = null;
        scanner.Input = TextureToMat(inputTexture);

        // should we fail, there is second try - HSV might help to detect paper by color difference
        if (!scanner.Success)
            // this will drop current result and re-fetch it next time we query for 'Success' flag or actual data
            scanner.Settings.GrayMode = PaperScanner.ScannerSettings.ColorMode.HueGrayscale;

        // now can combine Original/Scanner image

        return scanner.PaperShape;
    }

    // Use this for initialization
    public Mat Process2(Texture2D name)
    {
        Texture2D inputTexture = (Texture2D)name;

        // first of all, we set up scan parameters
        // 
        // scanner.Settings has more values than we use
        // (like Settings.Decolorization that defines
        // whether b&w filter should be applied), but
        // default values are quite fine and some of
        // them are by default in "smart" mode that
        // uses heuristic to find best choice. so,
        // we change only those that matter for us
        scanner.Settings.NoiseReduction = 0.7;                                          // real-world images are quite noisy, this value proved to be reasonable
        scanner.Settings.EdgesTight = 0.9;                                              // higher value cuts off "noise" as well, this time smaller and weaker edges
        scanner.Settings.ExpectedArea = 0.2;                                            // we expect document to be at least 20% of the total image area
        scanner.Settings.GrayMode = PaperScanner.ScannerSettings.ColorMode.Grayscale;   // color -> grayscale conversion mode

        // process input with PaperScanner
        Mat result = null;
        scanner.Input = TextureToMat(inputTexture);

        // should we fail, there is second try - HSV might help to detect paper by color difference
        if (!scanner.Success)
            // this will drop current result and re-fetch it next time we query for 'Success' flag or actual data
            scanner.Settings.GrayMode = PaperScanner.ScannerSettings.ColorMode.HueGrayscale;

        // now can combine Original/Scanner image

        return scanner.Output;
    }


    public void StartButtonFunction()
    {
        Stop = true;
    }

    public void CancelButtonFunction()
    {
        background.rectTransform.sizeDelta = new Vector2(backCam.width, backCam.height);
        StopAllCoroutines();
        Stop = false;
        Stop2 = false;
        tesseract.displayText.text = "";
        foreach (Transform child in background.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }


    IEnumerator contour()
    {
        t.SetPixels(backCam.GetPixels());
        t.Apply();

        pts = Process(t);
        tt = TextureToMat(t);


        Cv2.Line(tt, pts[0], pts[1], color, 7);
        Cv2.Line(tt, pts[1], pts[2], color, 7);
        Cv2.Line(tt, pts[2], pts[3], color, 7);
        Cv2.Line(tt, pts[3], pts[0], color, 7);


        Destroy(test);

        test = new Texture2D(background.texture.width, background.texture.height);
        test = MatToTexture(tt, test);


        background.texture = test;
        tt.Release();

        yield return null;
    }

    IEnumerator StartB()
    {
        Stop2 = true;
        t.SetPixels(backCam.GetPixels());
        t.Apply();

        Mat p = Process2(t);

        Destroy(test);
        test = new Texture2D(background.texture.width, background.texture.height);

        test = MatToTexture(p, test);

        background.rectTransform.sizeDelta = new Vector2(test.width, test.height);

        background.texture = test;

        float rapport = (float)Screen.height / (float)test.height;

        tesseract.rapport = rapport;
        tesseract.imageToRecognize = test;
        tesseract.Launch();

        //tesseract.displayText.text = "";
        //tesseract.displayText.text = tesseract._tesseractDriver._tesseract.text_size.ToString();
        //tesseract.displayText.fontSize = tesseract._tesseractDriver._tesseract.text_size * rapport;

        tesseract.displayText.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
        tesseract.displayText.rectTransform.position = new Vector3(tesseract.displayText.rectTransform.sizeDelta.x/2, tesseract.displayText.rectTransform.sizeDelta.y/2, 0);
        //tesseract.displayText.transform.position = new Vector3(0,0,0);
        tesseract.displayText.fontSize = tesseract._tesseractDriver._tesseract.text_size;

        //tesseract.displayText.fontSize = tesseract._tesseractDriver._tesseract.text_size * rapport;
        //tesseract.displayText.text = rapport.ToString() + " " + tesseract._tesseractDriver._tesseract.text_size.ToString();

        //tesseract.displayText.text = (tesseract.displayText.rectTransform.sizeDelta.x/2).ToString() + " " + (tesseract.displayText.rectTransform.sizeDelta.y / 2).ToString() + " " + test.height.ToString() + " " + test.width.ToString(); 

        yield return null;
    }

}
