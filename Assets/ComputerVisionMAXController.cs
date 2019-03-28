//-----------------------------------------------------------------------
// <copyright file="ComputerVisionController.cs" company="Google">
//
// Copyright 2017 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

namespace GoogleARCore.MAX
{
    using System;
    using System.Collections.Generic;
    using GoogleARCore;
    using UnityEngine;
    using UnityEngine.UI;

#if UNITY_EDITOR
    // Set up touch input propagation while using Instant Preview in the editor.
    using Input = InstantPreviewInput;
#endif  // UNITY_EDITOR

    /// <summary>
    /// Controller for the ComputerVision example that accesses the CPU camera image (i.e. image bytes), performs
    /// edge detection on the image, and renders an overlay to the screen.
    /// </summary>
    public class ComputerVisionMAXController : MonoBehaviour
    {
        /// <summary>
        /// The ARCoreSession monobehavior that manages the ARCore session.
        /// </summary>
        public ARCoreSession ARSessionManager;


        /// <summary>
        /// A Text box that is used to show messages at runtime.
        /// </summary>
        public Text SnackbarText;


        /// <summary>
        /// A buffer that stores the result of performing edge detection on the camera image each frame.
        /// </summary>
        private byte[] m_RawImage = null;
        private byte[] m_ResultImage = null;

        /// <summary>
        /// Texture created from the result of running edge detection on the camera image bytes.
        /// </summary>
        //private Texture2D m_EdgeDetectionBackgroundTexture = null;


        private bool m_IsQuitting = false;
        private ARCoreSession.OnChooseCameraConfigurationDelegate m_OnChoseCameraConfiguration = null;

        public delegate void OnImageAvailableCallbackFunc(byte[] buffer, int width, int hight);

        /// <summary>
        /// Callback function handle for receiving the output images.
        /// </summary>
        public event OnImageAvailableCallbackFunc OnImageAvailableCallback = null;


        /// <summary>
        /// The Unity Start() method.
        /// </summary>
        public void Start()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;


            var config = ARSessionManager.SessionConfig;
            SnackbarText.text = string.Empty;
#if UNITY_EDITOR
            SnackbarText.text =
                "Use mouse/keyboard in the editor Game view to toggle settings.\n" +
                "(Tapping on the device screen will not work while running in the editor)";


            if (config != null)
            {
                config.CameraFocusMode = CameraFocusMode.Fixed;
            }
#else
            if (config != null)
            {
                config.CameraFocusMode = CameraFocusMode.Auto;
            }
#endif

            // Register the callback to set camera config before arcore session is enabled.
            m_OnChoseCameraConfiguration = _ChooseCameraConfiguration;
            ARSessionManager.RegisterChooseCameraConfigurationCallback(m_OnChoseCameraConfiguration);

            ARSessionManager.enabled = true;
        }

        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update()
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }

            _QuitOnConnectionErrors();

            if (!Session.Status.IsValid())
            {
                return;
            }

            using (var image = Frame.CameraImage.AcquireCameraImageBytes())
            {
                if (!image.IsAvailable)
                {
                    return;
                }

                _OnImageAvailable(image.Width, image.Height, image.YRowStride, image.Y, 0);
            }

        }


        /// <summary>
        /// Handles a new CPU image.
        /// </summary>
        /// <param name="width">Width of the image, in pixels.</param>
        /// <param name="height">Height of the image, in pixels.</param>
        /// <param name="rowStride">Row stride of the image, in pixels.</param>
        /// <param name="pixelBuffer">Pointer to raw image buffer.</param>
        /// <param name="bufferSize">The size of the image buffer, in bytes.</param>
        private void _OnImageAvailable(int width, int height, int rowStride, IntPtr pixelBuffer, int bufferSize)
        {

            if (m_RawImage == null || m_RawImage.Length != width * height)
            {
                m_RawImage = new byte[width * height];
            }
            if (m_ResultImage == null || m_ResultImage.Length != width * height)
            {
                m_ResultImage = new byte[width * height];
            }

            // Detect edges within the image.
            OnImageAvailableCallback?.Invoke(m_ResultImage, width, height);
        }

        private void GetImgBuffer(byte[] outputImage, IntPtr inputImage, int width, int height, int rowStride)
        {
            // Adjust buffer size if necessary.
            int bufferSize = rowStride * height;

            // Move raw data into managed buffer.
            System.Runtime.InteropServices.Marshal.Copy(inputImage, m_RawImage, 0, bufferSize);

            for (int j = 1; j < height - 1; j++)
            {
                for (int i = 1; i < width - 1; i++)
                {
                    int offset = (j * rowStride) + i;
                    outputImage[(j * width) + i] = m_RawImage[offset];
                }
            }
        }


        /// <summary>
        /// Quit the application if there was a connection error for the ARCore session.
        /// </summary>
        private void _QuitOnConnectionErrors()
        {
            if (m_IsQuitting)
            {
                return;
            }

            // Quit if ARCore was unable to connect and give Unity some time for the toast to appear.
            if (Session.Status == SessionStatus.ErrorPermissionNotGranted)
            {
                _ShowAndroidToastMessage("Camera permission is needed to run this application.");
                m_IsQuitting = true;
                Invoke("_DoQuit", 0.5f);
            }
            else if (Session.Status == SessionStatus.FatalError)
            {
                _ShowAndroidToastMessage("ARCore encountered a problem connecting.  Please start the app again.");
                m_IsQuitting = true;
                Invoke("_DoQuit", 0.5f);
            }
        }

        /// <summary>
        /// Show an Android toast message.
        /// </summary>
        /// <param name="message">Message string to show in the toast.</param>
        private void _ShowAndroidToastMessage(string message)
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
        private void _DoQuit()
        {
            Application.Quit();
        }

        /// <summary>
        /// Select the desired camera configuration.
        /// </summary>
        /// <param name="supportedConfigurations">A list of all supported camera configuration.</param>
        /// <returns>The desired configuration index.</returns>
        private int _ChooseCameraConfiguration(List<CameraConfig> supportedConfigurations)
        {
            //if (!m_Resolutioninitialized)
            //{
            //    Vector2 ImageSize = supportedConfigurations[0].ImageSize;
            //    LowResConfigToggle.GetComponentInChildren<Text>().text = string.Format(
            //        "Low Resolution CPU Image ({0} x {1})", ImageSize.x, ImageSize.y);
            //    ImageSize = supportedConfigurations[supportedConfigurations.Count - 1].ImageSize;
            //    HighResConfigToggle.GetComponentInChildren<Text>().text = string.Format(
            //        "High Resolution CPU Image ({0} x {1})", ImageSize.x, ImageSize.y);

            //    m_Resolutioninitialized = true;
            //}

            //if (m_UseHighResCPUTexture)
            //{
            //    return supportedConfigurations.Count - 1;
            //}

            return 0;
        }
    }
}
