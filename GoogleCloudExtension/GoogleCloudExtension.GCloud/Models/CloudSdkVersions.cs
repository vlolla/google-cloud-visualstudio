﻿// Copyright 2017 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace GoogleCloudExtension.GCloud.Models
{
    /// <summary>
    /// This class represents the various versions of components installed for the Cloud SDK.
    /// </summary>
    public class CloudSdkVersions
    {
        /// <summary>
        /// The version of the Cloud SDK itself.
        /// </summary>
        [JsonProperty("Google Cloud SDK")]
        [JsonConverter(typeof(VersionConverter))]
        public Version SdkVersion { get; set; }
    }
}
