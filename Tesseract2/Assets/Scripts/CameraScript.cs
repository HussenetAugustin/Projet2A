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
    public RawImage background; // Image contenant l'image capturée par la caméra
    public RawImage result; // Image contenant l'image extraite de l'image initiale après lui avoir fait subir une transformation homographique pour la projeter dans le plan du téléphone
    public TesseractDemoScript tesseract; // Objet contenant le script tesseract permettant d'extraire le texte d'une image
    public TMP_Dropdown options; // Bouton contenant les différentes options d'effet dyslexique
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
    private bool anim;
    private float etendue;
    private float vitesse;
    private Texture2D Im;

    // Start is called before the first frame update
    void Start()
    {
        Im = Resources.Load("Images/Image2") as Texture2D;

        // On active l'animation par défaut
        anim = true;
        options.value = effet.value;
        result.gameObject.SetActive(false);
        Stop = false;
        Stop2 = false;
        color = new Scalar(255, 0, 0);
        pts = new Point[4];
        tesseract.displayText.text = "";
        tesseract.outputImage = result;

        // Script permettant de rechercher une caméra sur l'appareil sur lequel on exécute l'application
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
        //background.texture = Im;
        background.rectTransform.sizeDelta = new Vector2(backCam.width, backCam.height);
        //background.rectTransform.sizeDelta = new Vector2(Im.width, Im.height);

        t = new Texture2D(background.texture.width, background.texture.height);
        test = new Texture2D(background.texture.width, background.texture.height);
        

        camAvailable = true;

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
    }

    // Update is called once per frame
    void Update()
    {
        if (!camAvailable)
        {
            return;
        }        
        //Tant qu'on a pas appuyé sur le bouton start
        if (!Stop)
        {
            StartCoroutine(contour());
        }
        //Quand on appuie sur le bouton start
        else if (!Stop2)
        {
            StartCoroutine(contour());
            StartCoroutine(StartB());
        }
    }

    //Fonction associé au bouton permettant de lancer ou de mettre en marche l'animation associé à l'effet dyslexique
    public void Animation()
    {
        // Si une animation est en cours, on l'arrête
        if (anim)
        {
            anim = false;
        }
        //Sinon, on lance l'animation choisie
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

    //Permet de retenir l'animation dyslexique selectionnée lors du rechargement de la scène
    public void Changement()
    {
        effet.value = options.value;
    }


    // Fonction qui lance l'algorithme d'OpenCV permettant de detecter dans l'image une forme quadrilatère plus claire que le reste de l'image 
    // La fonction renvoie les coordonnées du contour de cette partie de l'image
    public Point[] Process(Texture2D name)
    {
        Texture2D inputTexture = (Texture2D)name;
        scanner.Settings.NoiseReduction = 0.7;                                          // real-world images are quite noisy, this value proved to be reasonable
        scanner.Settings.EdgesTight = 0.9;                                              // higher value cuts off "noise" as well, this time smaller and weaker edges
        scanner.Settings.ExpectedArea = 0.2;                                            // we expect document to be at least 20% of the total image area
        scanner.Settings.GrayMode = PaperScanner.ScannerSettings.ColorMode.Grayscale;   // color -> grayscale conversion mode

        scanner.Input = TextureToMat(inputTexture);

        if (!scanner.Success)
            scanner.Settings.GrayMode = PaperScanner.ScannerSettings.ColorMode.HueGrayscale;

        return scanner.PaperShape;
    }

    // Fonction qui lance l'algorithme d'OpenCV permettant de detecter dans l'image une forme quadrilatère plus claire que le reste de l'image 
    // La fonction renvoie la matrice correspondant à cette partie de l'image après lui avoir fait subir une transformation homographique afin de la projeter sous forme rectangulaire dans le plan du téléphone
    public Mat Process2(Texture2D name)
    {
        Texture2D inputTexture = (Texture2D)name;

        scanner.Settings.NoiseReduction = 0.7;                                          // real-world images are quite noisy, this value proved to be reasonable
        scanner.Settings.EdgesTight = 0.9;                                              // higher value cuts off "noise" as well, this time smaller and weaker edges
        scanner.Settings.ExpectedArea = 0.2;                                            // we expect document to be at least 20% of the total image area
        scanner.Settings.GrayMode = PaperScanner.ScannerSettings.ColorMode.Grayscale;   // color -> grayscale conversion mode

        scanner.Input = TextureToMat(inputTexture);
        if (!scanner.Success)
            scanner.Settings.GrayMode = PaperScanner.ScannerSettings.ColorMode.HueGrayscale;

        //Mise en mémoire des dimensions de l'écran
        taille.x = Screen.width;
        taille.y = Screen.height;

        //Mise en mémoire des dimensions de l'objet contenant l'image de la caméra
        tailleImage.x = background.texture.width;
        tailleImage.y = background.texture.height;

        //On retient les positions du contour de la partie de l'image que l'on a extrait.
        position.drc = new Vector2(background.texture.width/2 - scanner.PaperShape[0].X, background.texture.height/2 - (background.texture.height - scanner.PaperShape[0].Y));
        position.dlc = new Vector2(background.texture.width/2 - scanner.PaperShape[1].X, background.texture.height/2 - (background.texture.height - scanner.PaperShape[1].Y));
        position.ulc = new Vector2(background.texture.width/2 - scanner.PaperShape[2].X, background.texture.height/2 - (background.texture.height - scanner.PaperShape[2].Y));
        position.urc = new Vector2(background.texture.width/2 - scanner.PaperShape[3].X, background.texture.height/2 - (background.texture.height - scanner.PaperShape[3].Y));

        return scanner.Output;
    }

    // Fonction associé au bouton Start qui permet de lancer le processus principal de l'application (extraction de la partie de l'image détécté, extraction du texte et mise en place d'effet dyslexique)
    public void StartButtonFunction()
    {
        Stop = true;
    }

    // Fonction asspcié au bouton Cancel qui permet de relancer la scène afin de pouvoir recommencer le processus avec une nouvelle image
    public void CancelButtonFunction()
    {
        backCam.Stop();
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    // Fonction qui est appelé à chaque Frame et qui detecte le contour d'une forme quadrilatère plus claire dans l'image et dessine ce contour dans l'image.
    IEnumerator contour()
    {
        // La dernière image capturée par la caméra est mise sous forme d'une Texture2D
        t.SetPixels(backCam.GetPixels());
        //t = Im;
        t.Apply();

        // Calcul des coordonnées du contour de la partie de l'image détecté
        pts = Process(t);
        tt = TextureToMat(t);

        // Le contour est tracé dans l'image capturée par la caméra
        Cv2.Line(tt, pts[0], pts[1], color, 7);
        Cv2.Line(tt, pts[1], pts[2], color, 7);
        Cv2.Line(tt, pts[2], pts[3], color, 7);
        Cv2.Line(tt, pts[3], pts[0], color, 7);

        Destroy(test);

        test = new Texture2D(background.texture.width, background.texture.height);
        test = MatToTexture(tt, test);
        
        // On affiche l'image de la caméra sur laquelle on a tracé le contour de la partie détectée.
        background.texture = test;
        tt.Release();

        yield return null;
    }

    // Fonction appelée lorsque l'on appuie sur le bouton start afin de lancer le processus permettant d'afficher un effet dyslexique en réalité augmentée
    IEnumerator StartB()
    {
        Stop2 = true;
        t.SetPixels(backCam.GetPixels());
        //t = Im;
        t.Apply();

        // Extraction de la partie de l'image détecté après lui avoir fait subir une transformation homographique pour la remettre dans le plan du téléphone.
        Mat p = Process2(t);

        test2 = new Texture2D(background.texture.width, background.texture.height);
        test2 = MatToTexture(p, test2);

        // On affiche l'image extraite dans l'objet result.
        result.texture = test2;

        result.gameObject.SetActive(true);
        result.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);


        float rapport = (float)Screen.height / (float)test2.height;

        // On lance l'algorithme de tesseract (contenu dans le gameObjet tesseract) sur la partie de l'image extraite de l'image originale
        // Tesseract reconnait et extrait le texte de l'image. 
        // Il supprime le texte reconnu de l'image et l'écrit dans des GameObjet de type textMeshPro u'il repositionne au bon emplacement devant l'image.
        tesseract.rapport = rapport;
        tesseract.imageToRecognize = test2;
        tesseract.Launch();

        tesseract.displayText.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
        tesseract.displayText.rectTransform.position = new Vector3(tesseract.displayText.rectTransform.sizeDelta.x/2, tesseract.displayText.rectTransform.sizeDelta.y/2, 0);
        tesseract.displayText.fontSize = tesseract._tesseractDriver._tesseract.text_size;
        
        projeter.gameObject.SetActive(true);
        result.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);

        yield return null;


        // Calcul permettant de régler l'étendue des effets dyslexique en fonction de la taille du texte de l'image.

        float siz = 0;
        int n = 0;

        foreach(Transform child in result.transform)
        {
            n++;
            siz += child.gameObject.GetComponent<TextMeshProUGUI>().fontSize;
        }

        etendue = siz / n;
        vitesse = 300f;


        //Lancement de l'effet dyslexique choisit
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

    // Animation dyslexique Hopping
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

    // Animation dyslexique Rotating
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

    // Animation dyslexique Upside-down
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

    // Animation dyslexique Backward
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

    // Animation dyslexique Trembling
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
