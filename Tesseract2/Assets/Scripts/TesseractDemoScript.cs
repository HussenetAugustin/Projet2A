using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TesseractDemoScript : MonoBehaviour
{
    public Texture2D imageToRecognize;
    public TextMeshProUGUI displayText;
    public RawImage outputImage;
    public TesseractDriver _tesseractDriver;
    public float rapport;
    public RawImage parent;
    public TextMeshProUGUI prefab;
    private string _text = "";
    private Texture2D _texture;
    private float textSize;
    
 
    public void Launch()
    {
        Texture2D texture = new Texture2D(imageToRecognize.width, imageToRecognize.height, TextureFormat.ARGB32, false);
        texture.SetPixels32(imageToRecognize.GetPixels32());
        texture.Apply();

        _tesseractDriver = new TesseractDriver();
        _tesseractDriver.rapport = rapport;
        _tesseractDriver.prefab = prefab;
        _tesseractDriver.parent = parent;
        _tesseractDriver.x = texture.width;
        _tesseractDriver.y = texture.height;
        Recoginze(texture);
    }

    private void Recoginze(Texture2D outputTexture)
    {
        _texture = outputTexture;
        ClearTextDisplay();
        AddToTextDisplay(_tesseractDriver.CheckTessVersion());
        _tesseractDriver.Setup(OnSetupCompleteRecognize);
    }

    private void OnSetupCompleteRecognize()
    {
        AddToTextDisplay(_tesseractDriver.Recognize(_texture));
        AddToTextDisplay(_tesseractDriver.GetErrorMessage(), true);
        SetImageDisplay();
    }

    private void ClearTextDisplay()
    {
        _text = "";
    }

    private void AddToTextDisplay(string text, bool isError = false)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        _text += (string.IsNullOrWhiteSpace(displayText.text) ? "" : "\n") + text;

        if (isError)
            Debug.LogError(text);
        else
            Debug.Log(text);
    }


    private void SetImageDisplay()
    {
        RectTransform rectTransform = outputImage.GetComponent<RectTransform>();
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            rectTransform.rect.width * _tesseractDriver.GetHighlightedTexture().height / _tesseractDriver.GetHighlightedTexture().width);
        outputImage.texture = _tesseractDriver.GetHighlightedTexture();
        displayText.text = _text;
    }
}