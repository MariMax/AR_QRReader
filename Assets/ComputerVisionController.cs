using System;
using System.Collections.Generic;
using GoogleARCore;
using UnityEngine;
using UnityEngine.UI;

//#if UNITY_EDITOR
//// Set up touch input propagation while using Instant Preview in the editor.
//using Input = GoogleARCore.InstantPreviewInput;
//#endif  // UNITY_EDITOR

/// <summary>
/// Controller for the ComputerVision example that accesses the CPU camera image (i.e. image bytes), performs
/// edge detection on the image, and renders an overlay to the screen.
/// </summary>
public class ComputerVisionController : MonoBehaviour
{
    /// <summary>
    /// The ARCoreSession monobehavior that manages the ARCore session.
    /// </summary>
    public ARCoreSession ARSessionManager;

    /// <summary>
    /// An image using a material with EdgeDetectionBackground.shader to render a
    /// percentage of the edge detection background to the screen over the standard camera background.
    /// </summary>
    public Image DebugBackground = null;

    /// <summary>
    /// A Text box that is used to show messages at runtime.
    /// </summary>
    public Text SnackbarText;

    /// <summary>
    /// A buffer that stores the result of performing edge detection on the camera image each frame.
    /// </summary>
    private byte[] ImageBuffer = null;

    /// <summary>
    /// Texture created from the result of running edge detection on the camera image bytes.
    /// </summary>
    private Texture2D DebugTexture = null;

    /// <summary>
    /// These UVs are applied to the background material to crop and rotate 'DebugTexture'
    /// to match the aspect ratio and rotation of the device display.
    /// </summary>
    private DisplayUvCoords CameraImageToDisplayUvTransformation;

    private ScreenOrientation? CachedOrientation = null;
    private Vector2 CachedScreenDimensions = Vector2.zero;
    private bool IsQuitting = false;
    private readonly float Delta = .01f;

    /// <summary>
    /// Callback function handle for receiving the output images.
    /// </summary>
    public delegate void OnImageAvailableCallbackFunc(byte[] buffer, int width, int hight);
    public event OnImageAvailableCallbackFunc OnImageAvailableCallback = null;


    /// <summary>
    /// The Unity Start() method.
    /// </summary>
    public void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        var config = ARSessionManager.SessionConfig;

#if UNITY_EDITOR
        if (config != null)
        {
            config.CameraFocusMode = CameraFocusMode.Fixed;
        }

#else
            SnackbarText.text = string.Empty;
            if (config != null)
            {
                config.CameraFocusMode = CameraFocusMode.Auto;
            }
#endif

        // Register the callback to set camera config before arcore session is enabled.
        ARSessionManager.RegisterChooseCameraConfigurationCallback((List<CameraConfig> supportedConfigurations) => { return 0; });
        ARSessionManager.enabled = true;
    }

    /// <summary>
    /// The Unity Update() method.
    /// </summary>
    public void Update()
    {
        QuitOnConnectionErrors();

        if (!Session.Status.IsValid() || OnImageAvailableCallback == null)
        {
            return;
        }

        using (var image = Frame.CameraImage.AcquireCameraImageBytes())
        {
            if (!image.IsAvailable)
            {
                return;
            }

            OnImageAvailable(image.Width, image.Height, image.YRowStride, image.Y);
        }
    }

    /// <summary>
    /// Handles a new CPU image.
    /// </summary>
    /// <param name="width">Width of the image, in pixels.</param>
    /// <param name="height">Height of the image, in pixels.</param>
    /// <param name="rowStride">Row stride of the image, in pixels.</param>
    /// <param name="pixelBuffer">Pointer to raw image buffer.</param>
    private void OnImageAvailable(int width, int height, int rowStride, IntPtr pixelBuffer)
    {
        if (DebugBackground != null && !DebugBackground.enabled || OnImageAvailableCallback == null)
        {
            return;
        }

        if (DebugTexture == null || ImageBuffer == null ||
            DebugTexture.width != width || DebugTexture.height != height)
        {
            DebugTexture = new Texture2D(width, height, TextureFormat.R8, false, false);
            ImageBuffer = new byte[width * height];
            CameraImageToDisplayUvTransformation = Frame.CameraImage.ImageDisplayUvs;
        }

        if (CachedOrientation != Screen.orientation || Mathf.Abs(CachedScreenDimensions.x - Screen.width) < Delta ||
            Mathf.Abs(CachedScreenDimensions.y - Screen.height) < Delta)
        {
            CameraImageToDisplayUvTransformation = Frame.CameraImage.ImageDisplayUvs;
            CachedOrientation = Screen.orientation;
            CachedScreenDimensions = new Vector2(Screen.width, Screen.height);
        }

        if (ImageProcessor.ProcessImage(ImageBuffer, pixelBuffer, width, height, rowStride))
        {
            DebugTexture.LoadRawTextureData(ImageBuffer);
            DebugTexture.Apply();

            OnImageAvailableCallback.Invoke(ImageBuffer, width, height);

            #region RenderCameraStreem
            if (DebugBackground != null)
            {
                DebugBackground.material.SetTexture("_ImageTex", DebugTexture);

                const string TOP_LEFT_RIGHT = "_UvTopLeftRight";
                const string BOTTOM_LEFT_RIGHT = "_UvBottomLeftRight";
                DebugBackground.material.SetVector(TOP_LEFT_RIGHT, new Vector4(
                    CameraImageToDisplayUvTransformation.TopLeft.x,
                    CameraImageToDisplayUvTransformation.TopLeft.y,
                    CameraImageToDisplayUvTransformation.TopRight.x,
                    CameraImageToDisplayUvTransformation.TopRight.y));
                DebugBackground.material.SetVector(BOTTOM_LEFT_RIGHT, new Vector4(
                    CameraImageToDisplayUvTransformation.BottomLeft.x,
                    CameraImageToDisplayUvTransformation.BottomLeft.y,
                    CameraImageToDisplayUvTransformation.BottomRight.x,
                    CameraImageToDisplayUvTransformation.BottomRight.y));

            }
            #endregion
        }
    }
    #region Quit If Session is not initialized
    /// <summary>
    /// Quit the application if there was a connection error for the ARCore session.
    /// </summary>
    private void QuitOnConnectionErrors()
    {
        if (IsQuitting)
        {
            return;
        }

        // Quit if ARCore was unable to connect and give Unity some time for the toast to appear.
        if (Session.Status == SessionStatus.ErrorPermissionNotGranted)
        {
            ShowAndroidToastMessage("Camera permission is needed to run this application.");
            IsQuitting = true;
            Invoke("Quit", 0.5f);
        }
        else if (Session.Status == SessionStatus.FatalError)
        {
            ShowAndroidToastMessage("ARCore encountered a problem connecting.  Please start the app again.");
            IsQuitting = true;
            Invoke("Quit", 0.5f);
        }
    }

    /// <summary>
    /// Show an Android toast message.
    /// </summary>
    /// <param name="message">Message string to show in the toast.</param>
    private void ShowAndroidToastMessage(string message)
    {
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        if (unityActivity != null)
        {
            AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
            unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity,
                    message, 0);
                toastObject.Call("show");
            }));
        }
    }

    /// <summary>
    /// Actually quit the application.
    /// </summary>
    private void Quit()
    {
        Application.Quit();
    }
    #endregion
}
