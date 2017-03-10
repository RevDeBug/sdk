using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.NET.Build.Tasks
{
    /// <summary>
    /// Based on: https://github.com/dotnet/core-setup/blob/master/src/Microsoft.Extensions.DependencyModel/DependencyContextWriter.cs.
    /// </summary>
    internal class DependencyContextRewriter
    {
        public DependencyContextRewriter(DependencyContext context, IList<ReferenceInfo> revDeBugAddons)
        {
            _context = context;
            _revDeBugAddons = revDeBugAddons;
        }

        public void Rewrite(MemoryStream existingJson, Stream outputStream)
        {
            using (var streamReader = new StreamReader(new MemoryStream(existingJson.GetBuffer())))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                var jdoc = JObject.Load(jsonReader);

                using (var writer = new StreamWriter(outputStream))
                {
                    using (var jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented })
                    {
                        Rewrite(jdoc).WriteTo(jsonWriter);
                    }
                }
            }
        }

        public JObject Rewrite(JObject sourceObject)
        {
            var mainProjectRuntimeLibrary = _context.RuntimeLibraries.FirstOrDefault(l => l.Type == "project");

            if (mainProjectRuntimeLibrary != null)
            {
                var targets = sourceObject[DependencyContextStrings.TargetsPropertyName];
                if (targets != null)
                {
                    var targetFramework = targets[_context.Target.Framework];
                    if (targetFramework != null)
                    {
                        var targetAssemblyPropertyName = (mainProjectRuntimeLibrary.Name + DependencyContextStrings.VersionSeperator + mainProjectRuntimeLibrary.Version).ToLower();
                        var targetAssembly = targetFramework[targetAssemblyPropertyName];
                        if (targetAssembly != null)
                        {
                            var runtimes = targetAssembly[DependencyContextStrings.RuntimeAssembliesKey] as JObject;
                            if (runtimes != null)
                            {
                                foreach (var addon in _revDeBugAddons)
                                {
                                    runtimes.Add(new JProperty(addon.FileName, new JObject()));
                                }
                            }
                        }
                    }
                }
            }

            return sourceObject;
        }

        private DependencyContext _context;

        private IList<ReferenceInfo> _revDeBugAddons;

        /// <summary>
        /// From https://github.com/dotnet/core-setup/blob/master/src/Microsoft.Extensions.DependencyModel/DependencyContextStrings.cs
        /// </summary>
        internal class DependencyContextStrings
        {
            internal const char VersionSeperator = '/';

            internal const string CompileTimeAssembliesKey = "compile";

            internal const string RuntimeAssembliesKey = "runtime";

            internal const string NativeLibrariesKey = "native";

            internal const string RuntimeTargetPropertyName = "runtimeTarget";

            internal const string LibrariesPropertyName = "libraries";

            internal const string TargetsPropertyName = "targets";

            internal const string DependenciesPropertyName = "dependencies";

            internal const string Sha512PropertyName = "sha512";

            internal const string PathPropertyName = "path";

            internal const string HashPathPropertyName = "hashPath";

            internal const string TypePropertyName = "type";

            internal const string ServiceablePropertyName = "serviceable";

            internal const string CompilationOptionsPropertName = "compilationOptions";

            internal const string DefinesPropertyName = "defines";

            internal const string LanguageVersionPropertyName = "languageVersion";

            internal const string PlatformPropertyName = "platform";

            internal const string AllowUnsafePropertyName = "allowUnsafe";

            internal const string WarningsAsErrorsPropertyName = "warningsAsErrors";

            internal const string OptimizePropertyName = "optimize";

            internal const string KeyFilePropertyName = "keyFile";

            internal const string DelaySignPropertyName = "delaySign";

            internal const string PublicSignPropertyName = "publicSign";

            internal const string DebugTypePropertyName = "debugType";

            internal const string EmitEntryPointPropertyName = "emitEntryPoint";

            internal const string GenerateXmlDocumentationPropertyName = "xmlDoc";

            internal const string PortablePropertyName = "portable";

            internal const string RuntimeTargetNamePropertyName = "name";

            internal const string RuntimeTargetSignaturePropertyName = "signature";

            internal const string RuntimesPropertyName = "runtimes";

            internal const string RuntimeTargetsPropertyName = "runtimeTargets";

            internal const string RidPropertyName = "rid";

            internal const string AssetTypePropertyName = "assetType";

            internal const string RuntimeAssetType = "runtime";

            internal const string NativeAssetType = "native";

            internal const string ResourceAssembliesPropertyName = "resources";

            internal const string LocalePropertyName = "locale";

            internal const string CompilationOnlyPropertyName = "compileOnly";
        }
    }
}
