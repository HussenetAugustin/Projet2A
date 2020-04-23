using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TesseractWrapper
{
#if UNITY_EDITOR
    private const string TesseractDllName = "tesseract";
    private const string LeptonicaDllName = "tesseract";
#elif UNITY_ANDROID
    private const string TesseractDllName = "libtesseract.so";
    private const string LeptonicaDllName = "liblept.so";
#else
    private const string TesseractDllName = "tesseract";
    private const string LeptonicaDllName = "tesseract";
#endif

    public float text_size;
    public int n;
    public float rapport;
    public TextMeshProUGUI prefab;
    public RawImage parent;
    private IntPtr _tessHandle;
    private Texture2D _highlightedTexture;
    private string _errorMsg;
    private const float MinimumConfidence = 50;

    [DllImport(TesseractDllName)]
    private static extern IntPtr TessVersion();

    [DllImport(TesseractDllName)]
    private static extern IntPtr TessBaseAPICreate();

    [DllImport(TesseractDllName)]
    private static extern int TessBaseAPIInit3(IntPtr handle, string dataPath, string language);

    [DllImport(TesseractDllName)]
    private static extern void TessBaseAPIDelete(IntPtr handle);

    [DllImport(TesseractDllName)]
    private static extern void TessBaseAPISetImage(IntPtr handle, IntPtr imagedata, int width, int height,
        int bytes_per_pixel, int bytes_per_line);

    [DllImport(TesseractDllName)]
    private static extern void TessBaseAPISetImage2(IntPtr handle, IntPtr pix);

    [DllImport(TesseractDllName)]
    private static extern int TessBaseAPIRecognize(IntPtr handle, IntPtr monitor);

    [DllImport(TesseractDllName)]
    private static extern IntPtr TessBaseAPIGetUTF8Text(IntPtr handle);

    [DllImport(TesseractDllName)]
    private static extern void TessDeleteText(IntPtr text);

    [DllImport(TesseractDllName)]
    private static extern void TessBaseAPIEnd(IntPtr handle);

    [DllImport(TesseractDllName)]
    private static extern void TessBaseAPIClear(IntPtr handle);

    [DllImport(TesseractDllName)]
    private static extern IntPtr TessBaseAPIGetWords(IntPtr handle, IntPtr pixa);
    
    [DllImport(TesseractDllName)]
    private static extern IntPtr TessBaseAPIAllWordConfidences(IntPtr handle);

    public TesseractWrapper(float r, RawImage p, TextMeshProUGUI pre)
    {
        prefab = pre;
        rapport = r;
        parent = p;
        _tessHandle = IntPtr.Zero;
        n = 0;
        text_size = 0;
    }

    public string Version()
    {
        IntPtr strPtr = TessVersion();
        string tessVersion = Marshal.PtrToStringAnsi(strPtr);
        return tessVersion;
    }

    public string GetErrorMessage()
    {
        return _errorMsg;
    }

    public bool Init(string lang, string dataPath)
    {
        if (!_tessHandle.Equals(IntPtr.Zero))
            Close();

        try
        {
            _tessHandle = TessBaseAPICreate();
            if (_tessHandle.Equals(IntPtr.Zero))
            {
                _errorMsg = "TessAPICreate failed";
                return false;
            }

            if (string.IsNullOrWhiteSpace(dataPath))
            {
                _errorMsg = "Invalid DataPath";
                return false;
            }

            int init = TessBaseAPIInit3(_tessHandle, dataPath, lang);
            if (init != 0)
            {
                Close();
                _errorMsg = "TessAPIInit failed. Output: " + init;
                return false;
            }
        }
        catch (Exception ex)
        {
            _errorMsg = ex + " -- " + ex.Message;
            return false;
        }

        return true;
    }

    public string Recognize(Texture2D texture)
    {
        if (_tessHandle.Equals(IntPtr.Zero))
            return null;

        _highlightedTexture = texture;

        int width = _highlightedTexture.width;
        int height = _highlightedTexture.height;
        Color32[] colors = _highlightedTexture.GetPixels32();
        int count = width * height;
        int bytesPerPixel = 4;
        byte[] dataBytes = new byte[count * bytesPerPixel];
        int bytePtr = 0;

        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                int colorIdx = y * width + x;
                dataBytes[bytePtr++] = colors[colorIdx].r;
                dataBytes[bytePtr++] = colors[colorIdx].g;
                dataBytes[bytePtr++] = colors[colorIdx].b;
                dataBytes[bytePtr++] = colors[colorIdx].a;
            }
        }

        IntPtr imagePtr = Marshal.AllocHGlobal(count * bytesPerPixel);
        Marshal.Copy(dataBytes, 0, imagePtr, count * bytesPerPixel);

        TessBaseAPISetImage(_tessHandle, imagePtr, width, height, bytesPerPixel, width * bytesPerPixel);

        if (TessBaseAPIRecognize(_tessHandle, IntPtr.Zero) != 0)
        {
            Marshal.FreeHGlobal(imagePtr);
            return null;
        }
        
        IntPtr confidencesPointer = TessBaseAPIAllWordConfidences(_tessHandle);
        int i = 0;
        List<int> confidence = new List<int>();
        
        while (true)
        {
            int tempConfidence = Marshal.ReadInt32(confidencesPointer, i * 4);

            if (tempConfidence == -1) break;

            i++;
            confidence.Add(tempConfidence);
        }

        int pointerSize = Marshal.SizeOf(typeof(IntPtr));
        IntPtr intPtr = TessBaseAPIGetWords(_tessHandle, IntPtr.Zero);
        Boxa boxa = Marshal.PtrToStructure<Boxa>(intPtr);
        Box[] boxes = new Box[boxa.n];


        for (int index = 1; index < boxes.Length; index++)
        {
            if (confidence[index] >= MinimumConfidence)
            {
                IntPtr boxPtr = Marshal.ReadIntPtr(boxa.box, index * pointerSize);
                boxes[index] = Marshal.PtrToStructure<Box>(boxPtr);
                Box box = boxes[index];
                DrawLines(_highlightedTexture,
                    new Rect(box.x, _highlightedTexture.height - box.y - box.h, box.w, box.h),
                    Color.green);
                n += 1;
                text_size += box.h;
            }
        }
        text_size /= n;

        IntPtr stringPtr = TessBaseAPIGetUTF8Text(_tessHandle);
        Marshal.FreeHGlobal(imagePtr);
        if (stringPtr.Equals(IntPtr.Zero))
            return null;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        string recognizedText = Marshal.PtrToStringAnsi (stringPtr);
#else
        string recognizedText = Marshal.PtrToStringAuto(stringPtr);
#endif

        TessBaseAPIClear(_tessHandle);
        TessDeleteText(stringPtr);
        
        string[] words = recognizedText.Split(new[] {' ', '\n'}, StringSplitOptions.RemoveEmptyEntries);
        StringBuilder result = new StringBuilder();

        for (i = 0; i < boxes.Length; i++)
        {
            Debug.Log(words[i] + " -> " + confidence[i]);
            if (confidence[i] >= MinimumConfidence)
            {
                
                Box box = boxes[i];
                TextMeshProUGUI te = GameObject.Instantiate(prefab, new Vector3(0,0,0), Quaternion.identity);
                te.transform.SetParent(parent.transform);
                te.text = words[i];
                //te.fontSize = box.h;
                te.rectTransform.sizeDelta = new Vector2(box.w, box.h);
                te.rectTransform.position = new Vector3((Screen.width/2 - parent.rectTransform.sizeDelta.x/2) + box.x + te.rectTransform.sizeDelta.x / 2, (Screen.height/2 - parent.rectTransform.sizeDelta.y/2) + (parent.rectTransform.sizeDelta.y - box.y) - te.rectTransform.sizeDelta.y/2, 0);
                /*
                te.rectTransform.sizeDelta = new Vector2(box.w*rapport,box.h*rapport);
                te.rectTransform.position = new Vector3((box.x*rapport)+te.rectTransform.sizeDelta.x/2,(Screen.height-box.y*rapport)+te.rectTransform.sizeDelta.y/2, 0);
                */
                
                result.Append(words[i]);
                result.Append(" ");
            }
        }

        return result.ToString();
    }

    private void DrawLines(Texture2D texture, Rect boundingRect, Color color, int thickness = 3)
    {
        int x1 = (int) boundingRect.x;
        int x2 = (int) (boundingRect.x + boundingRect.width);
        int y1 = (int) boundingRect.y;
        int y2 = (int) (boundingRect.y + boundingRect.height);

        for(int x = x1; x <= x2; x++)
        {
            for (int y=y1; y<=y2; y++)
            {
                texture.SetPixel(x, y, color);
            }
        }

        /*
        for (int x = x1; x <= x2; x++)
        {
            for (int i = 0; i < thickness; i++)
            {
                texture.SetPixel(x, y1 + i, color);
                texture.SetPixel(x, y2 - i, color);
            }
        }

        for (int y = y1; y <= y2; y++)
        {
            for (int i = 0; i < thickness; i++)
            {
                texture.SetPixel(x1 + i, y, color);
                texture.SetPixel(x2 - i, y, color);
            }
        }
        */
        texture.Apply();
    }

    public Texture2D GetHighlightedTexture()
    {
        return _highlightedTexture;
    }

    public void Close()
    {
        if (_tessHandle.Equals(IntPtr.Zero))
            return;
        TessBaseAPIEnd(_tessHandle);
        TessBaseAPIDelete(_tessHandle);
        _tessHandle = IntPtr.Zero;
    }
}