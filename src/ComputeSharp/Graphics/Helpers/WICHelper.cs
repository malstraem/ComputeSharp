﻿using System;
using System.Diagnostics.Contracts;
using ComputeSharp.__Internals;
using ComputeSharp.Core.Extensions;
using TerraFX.Interop;
using FX = TerraFX.Interop.Windows;

#pragma warning disable CS0618

namespace ComputeSharp.Graphics.Helpers
{
    /// <summary>
    /// A <see langword="class"/> that uses the WIC APIs to load and decode images.
    /// </summary>
    internal sealed unsafe class WICHelper
    {
        /// <summary>
        /// The <see cref="IWICImagingFactory2"/> instance to use to create decoders.
        /// </summary>
        private readonly ComPtr<IWICImagingFactory2> wicImagingFactory2;

        /// <summary>
        /// Creates a new <see cref="WICHelper"/> instance.
        /// </summary>
        private WICHelper()
        {
            using ComPtr<IWICImagingFactory2> wicImagingFactory2 = default;
            Guid wicImagingFactor2Clsid = FX.CLSID_WICImagingFactory2;

            FX.CoCreateInstance(
                &wicImagingFactor2Clsid,
                null,
                (uint)CLSCTX.CLSCTX_INPROC_SERVER,
                FX.__uuidof<IWICImagingFactory2>(),
                wicImagingFactory2.GetVoidAddressOf()).Assert();

            this.wicImagingFactory2 = wicImagingFactory2.Move();
        }

        /// <summary>
        /// Destroys the current <see cref="WICHelper"/> instance.
        /// </summary>
        ~WICHelper()
        {
            this.wicImagingFactory2.Dispose();
        }

        /// <summary>
        /// Gets a <see cref="WICHelper"/> instance to use.
        /// </summary>
        public static WICHelper Instance { get; } = new();

        /// <summary>
        /// Loads a <see cref="ReadOnlyTexture2D{T, TPixel}"/> from a specified file.
        /// </summary>
        /// <typeparam name="T">The type of items to store in the texture.</typeparam>
        /// <typeparam name="TPixel">The type of pixels used on the GPU side.</typeparam>
        /// <param name="device">The <see cref="GraphicsDevice"/> instance to use to allocate the texture.</param>
        /// <param name="filename">The filename of the image file to load and decode into the texture.</param>
        /// <returns>A <see cref="ReadOnlyTexture2D{T, TPixel}"/> instance with the contents of the specified file.</returns>
        [Pure]
        public ReadOnlyTexture2D<T, TPixel> LoadTexture<T, TPixel>(GraphicsDevice device, ReadOnlySpan<char> filename)
            where T : unmanaged, IUnorm<TPixel>
            where TPixel : unmanaged
        {
            using ComPtr<IWICBitmapDecoder> wicBitmapDecoder = default;

            // Get the bitmap decoder for the target file
            fixed (char* p = filename)
            {
                this.wicImagingFactory2.Get()->CreateDecoderFromFilename(
                    (ushort*)p,
                    null,
                    FX.GENERIC_READ,
                    WICDecodeOptions.WICDecodeMetadataCacheOnDemand,
                    wicBitmapDecoder.GetAddressOf()).Assert();
            }

            using ComPtr<IWICBitmapFrameDecode> wicBitmapFrameDecode = default;

            // Get the first frame of the loaded image (if more are present, they will be ignored)
            wicBitmapDecoder.Get()->GetFrame(0, wicBitmapFrameDecode.GetAddressOf()).Assert();

            using ComPtr<IWICFormatConverter> wicFormatConverter = default;
            Guid wicPixelFormatGuid = WICFormatHelper.GetForType<T>();

            this.wicImagingFactory2.Get()->CreateFormatConverter(wicFormatConverter.GetAddressOf()).Assert();

            // Get a format converter to decode the pixel data
            wicFormatConverter.Get()->Initialize(
                (IWICBitmapSource*)wicBitmapFrameDecode.Get(),
                &wicPixelFormatGuid,
                WICBitmapDitherType.WICBitmapDitherTypeNone,
                null,
                0,
                WICBitmapPaletteType.WICBitmapPaletteTypeMedianCut).Assert();

            uint width, height;

            // Extract the image size info
            wicFormatConverter.Get()->GetSize(&width, &height).Assert();

            // Allocate an upload texture to transfer the decoded pixel data
            using UploadTexture2D<T> upload = device.AllocateUploadTexture2D<T>((int)width, (int)height);

            T* data = upload.View.DangerousGetAddressAndByteStride(out int strideInBytes);

            // Decode the pixel data into the upload buffer
            wicFormatConverter.Get()->CopyPixels(
                prc: null,
                cbStride: (uint)strideInBytes,
                cbBufferSize: (uint)strideInBytes * height,
                pbBuffer: (byte*)data).Assert();

            // Create the final texture to return
            ReadOnlyTexture2D<T, TPixel> texture = device.AllocateReadOnlyTexture2D<T, TPixel>((int)width, (int)height);

            upload.CopyTo(texture);

            return texture;
        }
    }
}
