using UnityEngine;
using ZXing;
using GoogleARCore;

public class QRReader : MonoBehaviour
{
    public GoogleARCore.Examples.ComputerVision.ComputerVisionController ImgHandler = null;
    private readonly IBarcodeReader BarcodeReader = new BarcodeReader();
    private Texture2D texture = null;
    private DisplayUvCoords m_CameraImageToDisplayUvTransformation;


    //public delegate void OnTextAvailableCallbackFunc(string text);
    //public event OnTextAvailableCallbackFunc OnTextAvailable = null;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Start");
        if (ImgHandler)
        {
            ImgHandler.OnImageAvailableCallback += HandleImage;
        }
    }

    void HandleImage(byte[] imgBuffer, int width, int height)
    {

        //if (texture == null || texture.width != width || texture.height != height)
        //{
        //    texture = new Texture2D(width, height, TextureFormat.R8, false, false);
        //}


        //texture.LoadRawTextureData(imgBuffer);
        //texture.Apply();

        DecodeQR(imgBuffer, width, height);
    }

    private void DecodeQR(byte[] imgBuffer, int width, int height)
    {
        Debug.Log("DECODE");
        var textResult = BarcodeReader.Decode(imgBuffer, width, height, RGBLuminanceSource.BitmapFormat.Gray8);
        if (textResult != null)
        {
            Debug.Log(textResult.Text);
        }


    }

    private void OnDestroy()
    {
        if (ImgHandler != null)
        {
            ImgHandler.OnImageAvailableCallback -= HandleImage;
        }
    }
}
