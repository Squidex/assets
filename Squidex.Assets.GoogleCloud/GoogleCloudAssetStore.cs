﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Google;
using Google.Cloud.Storage.V1;

namespace Squidex.Assets
{
    public sealed class GoogleCloudAssetStore : IAssetStore
    {
        private static readonly UploadObjectOptions IfNotExists = new UploadObjectOptions { IfGenerationMatch = 0 };
        private static readonly CopyObjectOptions IfNotExistsCopy = new CopyObjectOptions { IfGenerationMatch = 0 };
        private readonly string bucketName;
        private StorageClient storageClient;

        public GoogleCloudAssetStore(string bucketName)
        {
            AssetsGuard.NotNullOrEmpty(bucketName, nameof(bucketName));

            this.bucketName = bucketName;
        }

        public async Task InitializeAsync(CancellationToken ct = default)
        {
            try
            {
                storageClient = await StorageClient.CreateAsync();

                await storageClient.GetBucketAsync(bucketName, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                throw new AssetStoreException($"Cannot connect to google cloud bucket '{bucketName}'.", ex);
            }
        }

        public async Task<long> GetSizeAsync(string fileName, CancellationToken ct = default)
        {
            var name = GetFileName(fileName, nameof(fileName));

            try
            {
                var obj = await storageClient.GetObjectAsync(bucketName, name, null, ct);

                if (!obj.Size.HasValue)
                {
                    throw new AssetNotFoundException(fileName);
                }

                return (long)obj.Size.Value;
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                throw new AssetNotFoundException(fileName, ex);
            }
        }

        public async Task CopyAsync(string sourceFileName, string targetFileName, CancellationToken ct = default)
        {
            var sourceName = GetFileName(sourceFileName, nameof(sourceFileName));
            var targetName = GetFileName(targetFileName, nameof(targetFileName));

            try
            {
                await storageClient.CopyObjectAsync(bucketName, sourceName, bucketName, targetName, IfNotExistsCopy, ct);
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                throw new AssetNotFoundException(sourceName, ex);
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.PreconditionFailed)
            {
                throw new AssetAlreadyExistsException(targetFileName);
            }
        }

        public async Task DownloadAsync(string fileName, Stream stream, BytesRange range = default, CancellationToken ct = default)
        {
            var name = GetFileName(fileName, nameof(fileName));

            try
            {
                var downloadOptions = new DownloadObjectOptions();

                if (range.IsDefined)
                {
                    downloadOptions.Range = new RangeHeaderValue(range.From, range.To);
                }

                await storageClient.DownloadObjectAsync(bucketName, name, stream, downloadOptions, ct);
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                throw new AssetNotFoundException(fileName, ex);
            }
        }

        public async Task UploadAsync(string fileName, Stream stream, bool overwrite = false, CancellationToken ct = default)
        {
            var name = GetFileName(fileName, nameof(fileName));

            try
            {
                await storageClient.UploadObjectAsync(bucketName, name, "application/octet-stream", stream, overwrite ? null : IfNotExists, ct);
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.PreconditionFailed)
            {
                throw new AssetAlreadyExistsException(fileName);
            }
        }

        public async Task DeleteAsync(string fileName)
        {
            var name = GetFileName(fileName, nameof(fileName));

            try
            {
                await storageClient.DeleteObjectAsync(bucketName, name);
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                return;
            }
        }

        private static string GetFileName(string fileName, string parameterName)
        {
            AssetsGuard.NotNullOrEmpty(fileName, parameterName);

            return fileName.Replace("\\", "/");
        }
    }
}
