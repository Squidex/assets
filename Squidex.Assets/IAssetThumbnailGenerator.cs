﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.IO;
using System.Threading.Tasks;

namespace Squidex.Assets
{
    public interface IAssetThumbnailGenerator
    {
        Task<ImageInfo?> GetImageInfoAsync(Stream source);

        Task<ImageInfo> FixOrientationAsync(Stream source, Stream destination);

        Task CreateThumbnailAsync(Stream source, Stream destination, ResizeOptions options);
    }
}