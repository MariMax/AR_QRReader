using UnityEngine;
using UnityEngine.UI;
using ZXing;

public class QRReader : MonoBehaviour
{
    public Text SnackbarText;
    public ComputerVisionController ImgHandler = null;
    private readonly IBarcodeReader BarcodeReader = new BarcodeReader();

    public delegate void OnTextAvailableCallbackFunc(string text);
    public event OnTextAvailableCallbackFunc OnTextAvailable = null;

    // Start is called before the first frame update
    void Start()
    {
        if (ImgHandler)
        {
            ImgHandler.OnImageAvailableCallback += DecodeQR;
        }
    }

    private void DecodeQR(byte[] imgBuffer, int width, int height)
    {
        var textResult = BarcodeReader.Decode(imgBuffer, width, height, RGBLuminanceSource.BitmapFormat.Gray8);
        if (textResult != null)
        {
            if (SnackbarText != null)
            {
                SnackbarText.text = textResult.Text;
            }
            OnTextAvailable?.Invoke(textResult.Text);
        }
    }

    private void OnDestroy()
    {
        if (ImgHandler != null)
        {
            ImgHandler.OnImageAvailableCallback -= DecodeQR;
        }
    }
}
