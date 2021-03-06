﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Runtime.Serialization;

namespace Squidex.Assets
{
    [Serializable]
    public class AssetAlreadyExistsException : Exception
    {
        public AssetAlreadyExistsException(string fileName, Exception? inner = null)
            : base(FormatMessage(fileName), inner)
        {
        }

        protected AssetAlreadyExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        private static string FormatMessage(string fileName)
        {
            AssetsGuard.NotNullOrEmpty(fileName, nameof(fileName));

            return $"An asset with name '{fileName}' already exists.";
        }
    }
}
