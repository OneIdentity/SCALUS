// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScalusJson.cs" company="One Identity Inc.">
//   This software is licensed under the Apache 2.0 open source license.
//   https://github.com/OneIdentity/SCALUS/blob/master/LICENSE
//
//
//   Copyright One Identity LLC.
//   ALL RIGHTS RESERVED.
//
//   ONE IDENTITY LLC. MAKES NO REPRESENTATIONS OR
//   WARRANTIES ABOUT THE SUITABILITY OF THE SOFTWARE,
//   EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
//   TO THE IMPLIED WARRANTIES OF MERCHANTABILITY,
//   FITNESS FOR A PARTICULAR PURPOSE, OR
//   NON-INFRINGEMENT.  ONE IDENTITY LLC. SHALL NOT BE
//   LIABLE FOR ANY DAMAGES SUFFERED BY LICENSEE
//   AS A RESULT OF USING, MODIFYING OR DISTRIBUTING
//   THIS SOFTWARE OR ITS DERIVATIVES.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace OneIdentity.Scalus.Util
{
    using System.Text.Encodings.Web;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.Json.Serialization.Metadata;
    using OneIdentity.Scalus.Dto;
    using OneIdentity.Scalus.Verify;

    // Source-generated serialization metadata for the configuration model. Using a context keeps
    // the (de)serialization reflection-free so Scalus.Core stays trim/NativeAOT friendly. The disk
    // format is camelCase and indented; enums are written as their names (see the per-enum
    // JsonStringEnumConverter attributes) so the file stays human-readable.
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        WriteIndented = true)]
    [JsonSerializable(typeof(ScalusConfig))]
    [JsonSerializable(typeof(VerifyResult))]
    [JsonSerializable(typeof(LaunchRecord))]
    [JsonSerializable(typeof(UiWindowSettings))]
    internal partial class ScalusJsonContext : JsonSerializerContext
    {
    }

    // Shared System.Text.Json options for reading and writing SCALUS configuration on disk.
    internal static class ScalusJson
    {
        // Read/write the configuration file. camelCase + indented (baked into the source-gen
        // context), case-insensitive on read so legacy/hand-edited PascalCase files still load, and
        // the relaxed encoder so inline template content is written with minimal escaping.
        public static readonly JsonSerializerOptions Disk = Build(strict: false);

        // Same as Disk but rejects unknown members, used by the strict validation path.
        public static readonly JsonSerializerOptions DiskStrict = Build(strict: true);

        // Serialize using the value's runtime type so any ScalusConfig subtype is written whole,
        // matching the previous Newtonsoft behaviour. ScalusConfig is registered in the source-gen
        // context, so resolving the JsonTypeInfo from the options stays reflection-free (AOT-safe).
        public static string Serialize(ScalusConfig configuration) =>
            JsonSerializer.Serialize(configuration, Disk.GetTypeInfo(configuration.GetType()));

        public static string Serialize(VerifyResult result) =>
            JsonSerializer.Serialize(result, TypeInfo<VerifyResult>(Disk));

        public static string Serialize(LaunchRecord record) =>
            JsonSerializer.Serialize(record, TypeInfo<LaunchRecord>(Disk));

        public static string Serialize(UiWindowSettings settings) =>
            JsonSerializer.Serialize(settings, TypeInfo<UiWindowSettings>(Disk));

        public static ScalusConfig Deserialize(string json, bool strict = false) =>
            JsonSerializer.Deserialize(json, TypeInfo<ScalusConfig>(strict ? DiskStrict : Disk));

        public static LaunchRecord DeserializeLaunchRecord(string json) =>
            JsonSerializer.Deserialize(json, TypeInfo<LaunchRecord>(Disk));

        public static UiWindowSettings DeserializeUiWindowSettings(string json) =>
            JsonSerializer.Deserialize(json, TypeInfo<UiWindowSettings>(Disk));

        private static JsonTypeInfo<T> TypeInfo<T>(JsonSerializerOptions options) =>
            (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));

        private static JsonSerializerOptions Build(bool strict)
        {
            var options = new JsonSerializerOptions(ScalusJsonContext.Default.Options)
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };

            if (strict)
            {
                options.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
            }

            return options;
        }
    }
}
