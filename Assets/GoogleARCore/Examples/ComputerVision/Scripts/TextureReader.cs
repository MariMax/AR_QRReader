//-----------------------------------------------------------------------
// <copyright file="TextureReader.cs" company="Google">
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
namespace GoogleARCore.Examples.ComputerVision
{
    using System;
    using GoogleARCore;
    using UnityEngine;

    /// <summary>
    /// Component that provides CPU access to ArCore GPU texture.
    /// </summary>
    public class TextureReader : MonoBehaviour
    {
        /// <summary>
        /// Callback function type for receiving the output images.
        /// </summary>
        /// <param name="width">The width of the image, in pixels.</param>
        /// <param name="height">The height of the image, in pixels.</param>
        /// <param name="pixelBuffer">The pointer to the raw buffer of the image pixels.</param>
        /// <param name="rowStride">The size of the image buffer, in bytes.</param>
        public delegate void OnImageAvailableCallbackFunc(int width, int height, IntPtr pixelBuffer, int rowStride);

        /// <summary>
        /// Callback function handle for receiving the output images.
        /// </summary>
        public event OnImageAvailableCallbackFunc OnImageAvailableCallback = null;

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        public void Update()
        {
            if (!enabled || OnImageAvailableCallback == null)
            {
                return;
            }

            using (var image = Frame.CameraImage.AcquireCameraImageBytes())
            {
                if (!image.IsAvailable)
                {
                    return;
                }
                OnImageAvailableCallback(image.Width, image.Height, image.Y, image.YRowStride);
            }

        }

    }
}