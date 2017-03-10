﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;

namespace Microsoft.NET.Build.Tasks
{
    internal class ReferenceInfo
    {
        private const string RevDeBugAddonProperty = "RevDeBugAddon";

        public string Name { get; }
        public string Version { get; }
        public string FullPath { get; }
        public bool RDBAddon { get; }
        public string FileName => Path.GetFileName(FullPath);

        private List<ResourceAssemblyInfo> _resourceAssemblies;
        public IEnumerable<ResourceAssemblyInfo> ResourceAssemblies
        {
            get { return _resourceAssemblies; }
        }

        private ReferenceInfo(string name, string version, string fullPath, bool rdbAddon)
        {
            Name = name;
            Version = version;
            FullPath = fullPath;
            RDBAddon = rdbAddon;

            _resourceAssemblies = new List<ResourceAssemblyInfo>();
        }

        public static IEnumerable<ReferenceInfo> CreateFrameworkReferenceInfos(IEnumerable<ITaskItem> referencePaths)
        {
            IEnumerable<ITaskItem> frameworkReferencePaths = referencePaths
                .Where(r => r.GetBooleanMetadata("FrameworkFile") == true ||
                            r.GetMetadata("ResolvedFrom") == "ImplicitlyExpandDesignTimeFacades");

            List<ReferenceInfo> frameworkReferences = new List<ReferenceInfo>();
            foreach (ITaskItem frameworkReferencePath in frameworkReferencePaths)
            {
                frameworkReferences.Add(CreateReferenceInfo(frameworkReferencePath));
            }

            return frameworkReferences;
        }

        public static IEnumerable<ReferenceInfo> CreateDirectReferenceInfos(
            IEnumerable<ITaskItem> referencePaths,
            IEnumerable<ITaskItem> referenceSatellitePaths)
        {
            IEnumerable<ITaskItem> directReferencePaths = referencePaths
                .Where(r => r.HasMetadataValue("CopyLocal", "true") &&
                            r.HasMetadataValue("ReferenceSourceTarget", "ResolveAssemblyReference") &&
                            string.IsNullOrEmpty(r.GetMetadata("NuGetSourceType")));

            Dictionary<string, ReferenceInfo> directReferences = new Dictionary<string, ReferenceInfo>();
            foreach (ITaskItem directReferencePath in directReferencePaths)
            {
                ReferenceInfo referenceInfo = CreateReferenceInfo(directReferencePath);
                directReferences.Add(referenceInfo.FullPath, referenceInfo);
            }

            foreach (ITaskItem referenceSatellitePath in referenceSatellitePaths)
            {
                string originalItemSpec = referenceSatellitePath.GetMetadata("OriginalItemSpec");
                if (!string.IsNullOrEmpty(originalItemSpec))
                {
                    ReferenceInfo referenceInfo;
                    if (directReferences.TryGetValue(originalItemSpec, out referenceInfo))
                    {
                        ResourceAssemblyInfo resourceAssemblyInfo =
                            ResourceAssemblyInfo.CreateFromReferenceSatellitePath(referenceSatellitePath);
                        referenceInfo._resourceAssemblies.Add(resourceAssemblyInfo);
                    }
                }
            }

            return directReferences.Values;
        }

        private static ReferenceInfo CreateReferenceInfo(ITaskItem referencePath)
        {
            string fullPath = referencePath.ItemSpec;
            string name = Path.GetFileNameWithoutExtension(fullPath);
            string version = GetVersion(referencePath);
            bool? rdbAddon = referencePath.GetBooleanMetadata(RevDeBugAddonProperty);

            return new ReferenceInfo(name, version, fullPath, rdbAddon.HasValue ? rdbAddon.Value : false);
        }

        private static string GetVersion(ITaskItem referencePath)
        {
            string version = referencePath.GetMetadata("Version");

            if (string.IsNullOrEmpty(version))
            {
                string fusionName = referencePath.GetMetadata("FusionName");
                if (!string.IsNullOrEmpty(fusionName))
                {
                    AssemblyName assemblyName = new AssemblyName(fusionName);
                    version = assemblyName.Version.ToString();
                }

                if (string.IsNullOrEmpty(version))
                {
                    // Use 0.0.0.0 as placeholder, if we can't find a version any
                    // other way
                    version = "0.0.0.0";
                }
            }

            return version;
        }
    }
}
