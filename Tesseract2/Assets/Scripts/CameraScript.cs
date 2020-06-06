using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OpenCvSharp.Demo;
using OpenCvSharp;
using static OpenCvSharp.Unity;
using TMPro;
using System;
using UnityEngine.SceneManagement;

public class CameraScript : MonoBehaviour
{
    public RawImage background;
    public RawImage result;
    public TesseractDemoScript tesseract;
    public TMP_Dropdown options;
    public TextMeshProUGUI texte;
    public RawImage image;
    public Camera cam;
    public RawImage Testimage;
    public RenderTexture render;
    public Camera projeter;
    public Positions position;
    public Taille taille;
    public Taille tailleImage;
    public IntValue effet;

    private bool camAvailable;
    private bool coroutineEnd;
    private WebCamTexture backCam;
    private PaperScanner scanner = new PaperScanner();
    private Point[] pts;
    private Mat tt;
    private Scalar color;
    private Texture2D t;
    private Texture2D test;
    private Texture2D test2;
    private bool Stop;
    private bool Stop2;
    private Vector3 positionInitiale;
    private Vector2 tailleInitiale;
    private bool anim;
    private float etendue;
    private float vitesse;

    // Start is called before the first frame update
    void Start()
    {
        anim = true;
        options.value = effet.value;
        result.gameObject.SetActive(false);
        Stop = false;
        Stop2 = false;
        coroutineEnd = true;
        color = new Scalar(255, 0, 0);
        pts = new Point[4];
        tesseract.displayText.text = "";
        tesseract.outputImage = result;
        //positionInitiale = result.rectTransform.localPosition;
        tailleInitiale = result.rectTransform.sizeDelta;


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

        //Testimage.rectTransform.sizeDelta = new Vector2(background.texture.width, background.texture.height);
        Testimage.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
        Texture2D texture = new Texture2D(background.texture.width, background.texture.height, TextureFormat.ARGB32, false);
        Color[] pixels = texture.GetPixels(0, 0, texture.width, texture.height, 0);

        Color newcol = new Color(0, 0, 0, 0);
        Color newcol2 = new Color(255, 255, 255, 255);



        for (int p = 0; p < pixels.Length/2; p++)
        {
            pixels[p] = newcol;
        }
        for (int p = pixels.Length / 2; p < pixels.Length; p++)
        {
            pixels[p] = newcol2;
        }

        texture.SetPixels(0, 0, background.texture.width, background.texture.height, pixels, 0);
        texture.Apply();


        render = new RenderTexture(Screen.width, Screen.height, 24);
        Testimage.texture = render;

        cam.targetTexture = render;

        //Testimage.texture = texture;
        //cam.targetTexture = render;

    }

    // Update is called once per frame
    void Update()
    {
        //cam.targetTexture = render;


        if (!camAvailable)
        {
            return;
        }

        /*
        Input.gyro.enabled = true;

        double x = Math.Round((double)Input.gyro.attitude.eulerAngles.x, 3);
        double y = Math.Round((double)Input.gyro.attitude.eulerAngles.y, 3);
        double z = Math.Round((double)Input.gyro.attitude.eulerAngles.z, 3);

        texte.text = "x : " + x.ToString() + " / y : " + y.ToString() + " / z : " + z.ToString();

        Vector3 difOrientation = Input.gyro.attitude.eulerAngles - orientationCalibrage.eulerAngles;
        float difX = orientationCalibrage.eulerAngles.x - Input.gyro.attitude.eulerAngles.x;
        float difY = orientationCalibrage.eulerAngles.y - Input.gyro.attitude.eulerAngles.y;
        float difZ = orientationCalibrage.eulerAngles.z - Input.gyro.attitude.eulerAngles.z;

        //texte.rectTransform.rotation = Quaternion.Euler(-difX, difY, -difZ);
        //texte.rectTransform.rotation = Quaternion.Euler(difX, difY, - difZ);
        image.rectTransform.rotation = Quaternion.Euler(difX, difY, difZ);
        //texte.rectTransform.rotation = orientationCalibrage;
        //image.rectTransform.rotation = orientationCalibrage;
        */

        
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

    public void Animation()
    {
        if (anim)
        {
            anim = false;
        }
        else
        {
            anim = true;
            if (options.value == 0)
            {
                StartCoroutine(Hopping());
            }
            else if (options.value == 1)
            {
                StartCoroutine(Rotating());
            }
            else if (options.value == 2)
            {
                StartCoroutine(UpsideDown());
            }
            else if (options.value == 3)
            {
                StartCoroutine(Backward());
            }
            else if (options.value == 4)
            {
                StartCoroutine(Trembling());
            }
        }
    }

    public void Changement()
    {
        effet.value = options.value;
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

        taille.x = Screen.width;
        taille.y = Screen.height;

        tailleImage.x = background.texture.width;
        tailleImage.y = background.texture.height;

        Debug.Log(background.texture.width);
        Debug.Log(background.texture.height);

        position.drc = new Vector2(background.texture.width/2 - scanner.PaperShape[0].X, background.texture.height/2 - (background.texture.height - scanner.PaperShape[0].Y));
        position.dlc = new Vector2(background.texture.width/2 - scanner.PaperShape[1].X, background.texture.height/2 - (background.texture.height - scanner.PaperShape[1].Y));
        position.ulc = new Vector2(background.texture.width/2 - scanner.PaperShape[2].X, background.texture.height/2 - (background.texture.height - scanner.PaperShape[2].Y));
        position.urc = new Vector2(background.texture.width/2 - scanner.PaperShape[3].X, background.texture.height/2 - (background.texture.height - scanner.PaperShape[3].Y));


        return scanner.Output;
    }


    public void StartButtonFunction()
    {
        Stop = true;
    }

    public void CancelButtonFunction()
    {
        backCam.Stop();
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
        /*
        background.rectTransform.sizeDelta = new Vector2(backCam.width, backCam.height);
        //result.rectTransform.sizeDelta = tailleInitiale;
        //result.rectTransform.anchoredPosition = new Vector3(0,0,0);
        result.gameObject.SetActive(false);
        StopAllCoroutines();
        Stop = false;
        Stop2 = false;
        tesseract.displayText.text = "";
        foreach (Transform child in result.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        */
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

        Mat a = TextureToMat(t);

        Mat p = Process2(t);

        //Destroy(test);
        test2 = new Texture2D(background.texture.width, background.texture.height);

        test2 = MatToTexture(p, test2);

        //result.rectTransform.sizeDelta = new Vector2(test2.width, test2.height);
        //result.rectTransform.position = new Vector3(result.rectTransform.position.x + test2.width / 2 + pts[0].X, result.rectTransform.position.y - test2.height / 2 - (pts[0].Y + pts[1].Y) / 2, 0);
        result.texture = test2;
        Debug.Log(Screen.height);
        Debug.Log(result.rectTransform.sizeDelta.y);

        result.gameObject.SetActive(true);

        result.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);

        //background.rectTransform.sizeDelta = new Vector2(test.width, test.height);
        //background.texture = test;

        float rapport = (float)Screen.height / (float)test2.height;

        tesseract.rapport = rapport;
        tesseract.imageToRecognize = test2;
        tesseract.Launch();

        //tesseract.displayText.text = "";
        //tesseract.displayText.text = tesseract._tesseractDriver._tesseract.text_size.ToString();
        //tesseract.displayText.fontSize = tesseract._tesseractDriver._tesseract.text_size * rapport;

        
        tesseract.displayText.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
        tesseract.displayText.rectTransform.position = new Vector3(tesseract.displayText.rectTransform.sizeDelta.x/2, tesseract.displayText.rectTransform.sizeDelta.y/2, 0);
        //tesseract.displayText.transform.position = new Vector3(0,0,0);
        tesseract.displayText.fontSize = tesseract._tesseractDriver._tesseract.text_size;
        

        projeter.gameObject.SetActive(true);

        result.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);



        //tesseract.displayText.fontSize = tesseract._tesseractDriver._tesseract.text_size * rapport;
        //tesseract.displayText.text = rapport.ToString() + " " + tesseract._tesseractDriver._tesseract.text_size.ToString();

        //tesseract.displayText.text = (tesseract.displayText.rectTransform.sizeDelta.x/2).ToString() + " " + (tesseract.displayText.rectTransform.sizeDelta.y / 2).ToString() + " " + test.height.ToString() + " " + test.width.ToString(); 

        /*
        int width = test2.width;
        int height = test2.height;

        Point2f[] corners = new Point2f[]
            {
                new Point2f(0,     0),
                new Point2f(width, 0),
                new Point2f(width, height),
                new Point2f(0,     height)
            };
        Point2f[] destination = new Point2f[]
            {
                new Point2f(pts[0].X,pts[0].Y),
                new Point2f(pts[1].X,pts[1].Y),
                new Point2f(pts[2].X,pts[2].Y),
                new Point2f(pts[3].X,pts[3].Y)
            };

        var transform = Cv2.GetPerspectiveTransform(corners, destination);

        // un-warp
        a = a.WarpPerspective(transform, new Size(width, height), InterpolationFlags.Cubic);

        background.texture = MatToTexture(a);
        */


        yield return null;



        /*
        float anglez = Mathf.Atan(((float)(pts[0].Y - pts[1].Y) / (float)(pts[1].X - pts[0].X))) * (180.0f / Mathf.PI);
        Debug.Log(pts[0].X + "   " + pts[0].Y);
        Debug.Log(pts[1].X + "   " + pts[1].Y);
        Debug.Log(((float)(pts[0].Y - pts[1].Y) / (float)(pts[1].X - pts[0].X)));
        Debug.Log(Mathf.Atan(((float)(pts[1].Y - pts[0].Y) / (float)(pts[1].X - pts[0].X))));

        Debug.Log("angle : " + anglez);
        //result.rectTransform.eulerAngles = new Vector3(0,0,anglez);

        Input.gyro.enabled = true;

        float difX = Input.gyro.attitude.eulerAngles.x - orientationCalibrage.eulerAngles.x;
        float difY = Input.gyro.attitude.eulerAngles.y - orientationCalibrage.eulerAngles.y;
        float difZ = Input.gyro.attitude.eulerAngles.z - orientationCalibrage.eulerAngles.z;

        result.rectTransform.rotation = Quaternion.Euler(difX, difY, difZ);
        */


        float siz = 0;
        int n = 0;

        foreach(Transform child in result.transform)
        {
            Debug.Log("coucou");
            n++;
            siz += child.gameObject.GetComponent<TextMeshProUGUI>().fontSize;
        }

        etendue = siz / n;
        vitesse = 300f;

        if(options.value == 0)
        {
            StartCoroutine(Hopping());
        }
        else if (options.value == 1)
        {
            StartCoroutine(Rotating());
        }
        else if (options.value == 2)
        {
            StartCoroutine(UpsideDown());
        }
        else if (options.value == 3)
        {
            StartCoroutine(Backward());
        }
        else if (options.value == 4)
        {
            StartCoroutine(Trembling());
        }
    }

    IEnumerator Hopping()
    {
        while (anim)
        {
            List<float> positions = new List<float>();

            foreach (Transform child in result.transform)
            {
                positions.Add((UnityEngine.Random.Range(0, 2 * etendue) - etendue) / vitesse);
            }


            for (int i = 0; i < vitesse; i++)
            {
                int k = 0;
                foreach (Transform child in result.transform)
                {
                    Vector2 currentPosition = child.gameObject.GetComponent<RectTransform>().anchoredPosition;
                    child.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(currentPosition.x, currentPosition.y + positions[k]);
                    k++;
                }
                yield return null;
            }

            for (int i = 0; i < vitesse; i++)
            {
                int k = 0;
                foreach (Transform child in result.transform)
                {
                    Vector2 currentPosition = child.gameObject.GetComponent<RectTransform>().anchoredPosition;
                    child.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(currentPosition.x, currentPosition.y - positions[k]);
                    k++;
                }
                yield return null;
            }
        }
    }

    IEnumerator Rotating()
    {
        while (anim)
        {
            List<float> rotations = new List<float>();

            foreach (Transform child in result.transform)
            {
                rotations.Add((UnityEngine.Random.Range(0, 150) - 75) / vitesse);
            }


            for (int i = 0; i < vitesse; i++)
            {
                int k = 0;
                foreach (Transform child in result.transform)
                {
                    Vector3 currentOrientaion = child.gameObject.GetComponent<RectTransform>().rotation.eulerAngles;
                    child.gameObject.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, currentOrientaion.z + rotations[k]);
                    k++;
                }
                yield return null;
            }

            for (int i = 0; i < vitesse; i++)
            {
                int k = 0;
                foreach (Transform child in result.transform)
                {
                    Vector3 currentOrientaion = child.gameObject.GetComponent<RectTransform>().rotation.eulerAngles;
                    child.gameObject.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, currentOrientaion.z - rotations[k]);
                    k++;
                }
                yield return null;
            }
        }
    }

    IEnumerator UpsideDown()
    {
        List<int> positions = new List<int>();
        int j = 1;

        foreach (Transform child in result.transform)
        {
            positions.Add(j);
            j++;
        }


        while (anim)
        {

            List<int> alea = new List<int>();
            List<int> alea2 = new List<int>();

            for (int i = 0; i < positions.Count; i++)
            {
                alea.Add(positions[i]);
                alea2.Add(positions[i]);
            }

            for (int i = 0; i < alea.Count; i++)
            {
                int temp = alea[i];
                int randomIndex = UnityEngine.Random.Range(i, alea.Count);
                alea[i] = alea[randomIndex];
                alea[randomIndex] = temp;
            }

            for (int i = 0; i < alea.Count; i++)
            {
                alea2[alea2.Count - i - 1] = alea[i];
            }

            foreach (int numero in alea)
            {
                int temp = 1;
                foreach (Transform child in result.transform)
                {
                    if (temp == numero)
                    {
                        child.gameObject.GetComponent<RectTransform>().rotation = Quaternion.Euler(180, 0, 0);
                    }
                    temp++;
                }
                yield return new WaitForSecondsRealtime((float)(2f / positions.Count));
            }

            foreach (int numero in alea2)
            {
                int temp = 1;
                foreach (Transform child in result.transform)
                {
                    if (temp == numero)
                    {
                        child.gameObject.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, 0);
                    }
                    temp++;
                }
                yield return new WaitForSecondsRealtime((float)(2f / positions.Count));
            }

        }
    }

    IEnumerator Backward()
    {
        List<int> positions = new List<int>();
        int j = 1;

        foreach (Transform child in result.transform)
        {
            positions.Add(j);
            j++;
        }


        while (anim)
        {

            List<int> alea = new List<int>();
            List<int> alea2 = new List<int>();

            for (int i = 0; i < positions.Count; i++)
            {
                alea.Add(positions[i]);
                alea2.Add(positions[i]);
            }

            for (int i = 0; i < alea.Count; i++)
            {
                int temp = alea[i];
                int randomIndex = UnityEngine.Random.Range(i, alea.Count);
                alea[i] = alea[randomIndex];
                alea[randomIndex] = temp;
            }

            for (int i = 0; i < alea.Count; i++)
            {
                alea2[alea2.Count - i - 1] = alea[i];
            }

            foreach (int numero in alea)
            {
                int temp = 1;
                foreach (Transform child in result.transform)
                {
                    if (temp == numero)
                    {
                        child.gameObject.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 180, 0);
                    }
                    temp++;
                }
                yield return new WaitForSecondsRealtime((float)(2f / positions.Count));
            }

            foreach (int numero in alea2)
            {
                int temp = 1;
                foreach (Transform child in result.transform)
                {
                    if (temp == numero)
                    {
                        child.gameObject.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, 0);
                    }
                    temp++;
                }
                yield return new WaitForSecondsRealtime((float)(2f / positions.Count));
            }

        }
    }

    IEnumerator Trembling()
    {
        float v = 5f;
        while (anim)
        {
            List<float> distance = new List<float>();
            List<bool> droite = new List<bool>();

            foreach (Transform child in result.transform)
            {
                distance.Add((float)UnityEngine.Random.Range(0,30) / (v*10f));
                int a = UnityEngine.Random.Range(0, 1);
                if (a == 0) droite.Add(true);
                else droite.Add(false);
            }

            for(int j = 1; j < 11; j++)
            {
                for (int i = 0; i < v; i++)
                {
                    int k = 0;
                    foreach (Transform child in result.transform)
                    {
                        Vector2 currentPosition = child.gameObject.GetComponent<RectTransform>().anchoredPosition;
                        if (droite[k]) child.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(currentPosition.x + distance[k] * j, currentPosition.y);
                        else child.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(currentPosition.x - distance[k] * j, currentPosition.y);
                        k++;
                    }
                    yield return null;
                }

                for (int i = 0; i < v; i++)
                {
                    int k = 0;
                    foreach (Transform child in result.transform)
                    {
                        Vector2 currentPosition = child.gameObject.GetComponent<RectTransform>().anchoredPosition;
                        if (droite[k]) child.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(currentPosition.x - distance[k] * j, currentPosition.y);
                        else child.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(currentPosition.x + distance[k] * j, currentPosition.y);
                        k++;
                    }
                    yield return null;
                }

                for (int i = 0; i < v; i++)
                {
                    int k = 0;
                    foreach (Transform child in result.transform)
                    {
                        Vector2 currentPosition = child.gameObject.GetComponent<RectTransform>().anchoredPosition;
                        if (droite[k]) child.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(currentPosition.x - distance[k] * j, currentPosition.y);
                        else child.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(currentPosition.x + distance[k] * j, currentPosition.y);
                        k++;
                    }
                    yield return null;
                }

                for (int i = 0; i < v; i++)
                {
                    int k = 0;
                    foreach (Transform child in result.transform)
                    {
                        Vector2 currentPosition = child.gameObject.GetComponent<RectTransform>().anchoredPosition;
                        if (droite[k]) child.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(currentPosition.x + distance[k] * j, currentPosition.y);
                        else child.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(currentPosition.x - distance[k] * j, currentPosition.y);
                        k++;
                    }
                    yield return null;
                }
            }
            for (int j = 11; j > 0; j--)
            {
                for (int i = 0; i < v; i++)
                {
                    int k = 0;
                    foreach (Transform child in result.transform)
                    {
                        Vector2 currentPosition = child.gameObject.GetComponent<RectTransform>().anchoredPosition;
                        if (droite[k]) child.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(currentPosition.x + distance[k] * j, currentPosition.y);
                        else child.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(currentPosition.x - distance[k] * j, currentPosition.y);
                        k++;
                    }
                    yield return null;
                }

                for (int i = 0; i < v; i++)
                {
                    int k = 0;
                    foreach (Transform child in result.transform)
                    {
                        Vector2 currentPosition = child.gameObject.GetComponent<RectTransform>().anchoredPosition;
                        if (droite[k]) child.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(currentPosition.x - distance[k] * j, currentPosition.y);
                        else child.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(currentPosition.x + distance[k] * j, currentPosition.y);
                        k++;
                    }
                    yield return null;
                }

                for (int i = 0; i < v; i++)
                {
                    int k = 0;
                    foreach (Transform child in result.transform)
                    {
                        Vector2 currentPosition = child.gameObject.GetComponent<RectTransform>().anchoredPosition;
                        if (droite[k]) child.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(currentPosition.x - distance[k] * j, currentPosition.y);
                        else child.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(currentPosition.x + distance[k] * j, currentPosition.y);
                        k++;
                    }
                    yield return null;
                }

                for (int i = 0; i < v; i++)
                {
                    int k = 0;
                    foreach (Transform child in result.transform)
                    {
                        Vector2 currentPosition = child.gameObject.GetComponent<RectTransform>().anchoredPosition;
                        if (droite[k]) child.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(currentPosition.x + distance[k] * j, currentPosition.y);
                        else child.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(currentPosition.x - distance[k] * j, currentPosition.y);
                        k++;
                    }
                    yield return null;
                }
            }
        }
    }
}
