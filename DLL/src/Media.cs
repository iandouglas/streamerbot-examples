using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Streamer.bot.Plugin.Interface;

namespace iandouglas736
{
    /// <summary>
    /// Cross-platform media helpers for Streamer.bot actions.
    /// 
    /// The duration-detection helpers (LengthInSeconds, Length, IsMediaFile, etc.)
    /// do NOT require a Streamer.bot context — they work with local files only.
    /// 
    /// The PlayMp4InObs and PlayMp3 helpers DO require a context via SetContext(CPH)
    /// because they call Streamer.bot APIs to control OBS and play audio.
    /// 
    /// The default duration detection tries, in order:
    ///   1. NLayer + Duration.Mine.Mp4 DLLs if present in the Streamer.bot directory
    ///   2. ffprobe (FFmpeg) if installed and on PATH
    ///   3. Windows Shell Property System for common audio/video files
    ///   4. Falls back to null if no provider can read the file
    /// 
    /// You can also pass a custom provider to LengthInSeconds().
    /// </summary>
    public static class Media
    {
        private static IInlineInvokeProxy _cph;

        /// <summary>
        /// Required before using PlayMp4InObs or PlayMp3.
        /// The duration-detection helpers do NOT need a context.
        /// </summary>
        public static void SetContext(IInlineInvokeProxy cph)
        {
            _cph = cph ?? throw new ArgumentNullException(nameof(cph));
        }

        private static IInlineInvokeProxy CPH
        {
            get
            {
                if (_cph == null)
                    throw new InvalidOperationException("iandouglas736.Media.SetContext(CPH) must be called before using playback helpers.");
                return _cph;
            }
        }

        /// <summary>
        /// Plays an MP4 video file in an OBS media source. Determines the file's duration,
        /// makes the OBS source visible, sets the media source file, and hides the source
        /// after playback completes (or after the override duration, whichever is shorter).
        /// Logs an error via CPH.LogError if the file is missing, unplayable, or the
        /// duration cannot be determined and no override is provided.
        /// </summary>
        /// <param name="filename">Path to the .mp4 file.</param>
        /// <param name="obsScene">OBS scene name containing the media source.</param>
        /// <param name="obsSource">OBS media source name.</param>
        /// <param name="secondsDurationOverride">If &gt; 0, caps playback to this many seconds (file duration is used if shorter). If &lt;= 0, the full file duration is used.</param>
        public static void PlayMp4InObs(string filename, string obsScene, string obsSource, int secondsDurationOverride)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                CPH.LogError("[iandouglas736.Media.PlayMp4InObs] filename is null or empty.");
                return;
            }

            if (string.IsNullOrWhiteSpace(obsScene) || string.IsNullOrWhiteSpace(obsSource))
            {
                CPH.LogError("[iandouglas736.Media.PlayMp4InObs] obsScene and obsSource must be provided.");
                return;
            }

            string path = filename.Replace("\\", "/");
            if (!File.Exists(path))
            {
                CPH.LogError($"[iandouglas736.Media.PlayMp4InObs] File not found: {filename}");
                return;
            }

            double? durationSeconds = LengthInSeconds(path, DurationMineMp4Provider);
            double playSeconds;

            if (durationSeconds.HasValue && durationSeconds.Value > 0)
            {
                playSeconds = secondsDurationOverride > 0
                    ? Math.Min(durationSeconds.Value, secondsDurationOverride)
                    : durationSeconds.Value;
            }
            else if (secondsDurationOverride > 0)
            {
                playSeconds = secondsDurationOverride;
                CPH.LogError($"[iandouglas736.Media.PlayMp4InObs] Could not determine duration for '{filename}', using override of {secondsDurationOverride}s.");
            }
            else
            {
                CPH.LogError($"[iandouglas736.Media.PlayMp4InObs] Could not determine duration for '{filename}' and no override provided. Aborting playback.");
                return;
            }

            try
            {
                CPH.ObsSetMediaSourceFile(obsScene, obsSource, path);
                CPH.ObsSetSourceVisibility(obsScene, obsSource, true);
                CPH.Wait((int)(playSeconds * 1000));
                CPH.ObsSetSourceVisibility(obsScene, obsSource, false);
            }
            catch (Exception ex)
            {
                CPH.LogError($"[iandouglas736.Media.PlayMp4InObs] OBS playback failed for '{filename}': {ex.Message}");
            }
        }

        /// <summary>
        /// Plays an MP3 audio file through Streamer.bot's sound system. Determines the
        /// file's duration and waits for playback to complete (or for the override
        /// duration, whichever is shorter). Logs an error via CPH.LogError if the file
        /// is missing, unplayable, or the duration cannot be determined and no override
        /// is provided.
        /// </summary>
        /// <param name="filename">Path to the .mp3 file.</param>
        /// <param name="secondsDurationOverride">If &gt; 0, caps playback to this many seconds (file duration is used if shorter). If &lt;= 0, the full file duration is used.</param>
        public static void PlayMp3(string filename, int secondsDurationOverride)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                CPH.LogError("[iandouglas736.Media.PlayMp3] filename is null or empty.");
                return;
            }

            string path = filename.Replace("\\", "/");
            if (!File.Exists(path))
            {
                CPH.LogError($"[iandouglas736.Media.PlayMp3] File not found: {filename}");
                return;
            }

            double? durationSeconds = LengthInSeconds(path, NLayerMp3Provider);
            double playSeconds;

            if (durationSeconds.HasValue && durationSeconds.Value > 0)
            {
                playSeconds = secondsDurationOverride > 0
                    ? Math.Min(durationSeconds.Value, secondsDurationOverride)
                    : durationSeconds.Value;
            }
            else if (secondsDurationOverride > 0)
            {
                playSeconds = secondsDurationOverride;
                CPH.LogError($"[iandouglas736.Media.PlayMp3] Could not determine duration for '{filename}', using override of {secondsDurationOverride}s.");
            }
            else
            {
                CPH.LogError($"[iandouglas736.Media.PlayMp3] Could not determine duration for '{filename}' and no override provided. Aborting playback.");
                return;
            }

            try
            {
                CPH.PlaySound(path, 1.0f, true);
                int waitMs = (int)(playSeconds * 1000);
                if (waitMs > 0)
                    CPH.Wait(waitMs);
            }
            catch (Exception ex)
            {
                CPH.LogError($"[iandouglas736.Media.PlayMp3] Playback failed for '{filename}': {ex.Message}");
            }
        }
        /// <summary>
        /// Returns the media duration in seconds, or null if it cannot be determined.
        /// Supports common audio/video files such as .mp3, .mp4, .wav, .ogg, .webm, .mov, .mkv.
        /// </summary>
        public static double? LengthInSeconds(string filename)
        {
            return LengthInSeconds(filename, null);
        }

        /// <summary>
        /// Returns the media duration in seconds using an optional custom provider first.
        /// If the custom provider returns null, the default providers are tried.
        /// </summary>
        public static double? LengthInSeconds(string filename, Func<string, double?> customProvider)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return null;

            string path = filename.Replace("\\", "/");
            if (!File.Exists(path))
                return null;

            double? result = null;

            if (customProvider != null)
            {
                try { result = customProvider(path); } catch { result = null; }
                if (result.HasValue)
                    return result.Value;
            }

            // 1. Try bundled NLayer + Duration.Mine.Mp4 DLLs if available
            try { result = GetDurationViaBundledLibraries(path); } catch { result = null; }
            if (result.HasValue)
                return result.Value;

            // 2. Try ffprobe (works everywhere if ffmpeg is installed)
            try { result = GetDurationViaFfprobe(path); } catch { result = null; }
            if (result.HasValue)
                return result.Value;

            // 3. Try Windows Shell property system
            try { result = GetDurationViaShell(path); } catch { result = null; }
            if (result.HasValue)
                return result.Value;

            return null;
        }

        /// <summary>
        /// Returns the media duration as a TimeSpan, or null if unavailable.
        /// </summary>
        public static TimeSpan? Length(string filename)
        {
            double? seconds = LengthInSeconds(filename);
            return seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : (TimeSpan?)null;
        }

        /// <summary>
        /// Returns true if the file extension is a recognized audio/video container.
        /// </summary>
        public static bool IsMediaFile(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return false;
            string ext = Path.GetExtension(filename).ToLowerInvariant();
            string[] mediaExtensions = { ".mp3", ".mp4", ".wav", ".ogg", ".webm", ".mov", ".mkv", ".flac", ".aac", ".m4a", ".wma", ".avi", ".wmv", ".flv" };
            return mediaExtensions.Contains(ext);
        }

        /// <summary>
        /// If path points to a media file, returns it.
        /// If path points to a folder, returns a random media file from that folder.
        /// Returns null if no suitable file is found.
        /// </summary>
        public static string ResolveMediaFile(string path)
        {
            return ResolveMediaFile(path, null);
        }

        /// <summary>
        /// If path points to a media file, returns it.
        /// If path points to a folder, returns a random media file from that folder
        /// using the supplied Random instance (or a shared one if null).
        /// Returns null if no suitable file is found.
        /// </summary>
        public static string ResolveMediaFile(string path, Random random)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            // Keep UNC paths in Windows form (\\server\share) while normalizing
            // stray mixed separators everywhere else.
            string normalized = path.Replace('/', '\\').TrimEnd('\\');
            if (normalized.StartsWith("\\\\") && !path.StartsWith("\\\\"))
                normalized = "\\\\" + normalized.Substring(2);

            if (File.Exists(normalized))
            {
                return IsMediaFile(normalized) ? normalized : null;
            }

            if (Directory.Exists(normalized))
            {
                string[] files = Directory.GetFiles(normalized)
                    .Where(f => IsMediaFile(f))
                    .ToArray();

                if (files.Length == 0)
                    return null;

                Random rng = random ?? _sharedRandom;
                return files[rng.Next(files.Length)];
            }

            return null;
        }

        private static readonly Random _sharedRandom = new Random();

        /// <summary>
        /// Optional provider that uses NLayer.MpegFile for MP3 files.
        /// Pass this to LengthInSeconds(filename, Media.NLayerMp3Provider) if you want to force that path.
        /// </summary>
        public static double? NLayerMp3Provider(string filename)
        {
            string ext = Path.GetExtension(filename).ToLowerInvariant();
            if (ext != ".mp3")
                return null;

            return GetMp3DurationViaNLayer(filename);
        }

        /// <summary>
        /// Optional provider that uses Duration.Mine.Mp4.Mp4Duration for MP4 files.
        /// Pass this to LengthInSeconds(filename, Media.DurationMineMp4Provider) if you want to force that path.
        /// </summary>
        public static double? DurationMineMp4Provider(string filename)
        {
            string ext = Path.GetExtension(filename).ToLowerInvariant();
            if (ext != ".mp4")
                return null;

            return GetMp4DurationViaDurationMine(filename);
        }

        private static double? GetDurationViaBundledLibraries(string filename)
        {
            string ext = Path.GetExtension(filename).ToLowerInvariant();

            if (ext == ".mp3")
                return GetMp3DurationViaNLayer(filename);

            if (ext == ".mp4")
                return GetMp4DurationViaDurationMine(filename);

            return null;
        }

        private static double? GetMp3DurationViaNLayer(string filename)
        {
            Assembly nlayerAssembly = FindLoadedOrLocalAssembly("NLayer");
            if (nlayerAssembly == null)
                return null;

            try
            {
                Type mpegFileType = nlayerAssembly.GetType("NLayer.MpegFile");
                if (mpegFileType == null)
                    return null;

                ConstructorInfo ctor = mpegFileType.GetConstructor(new[] { typeof(string) });
                if (ctor == null)
                    return null;

                using (IDisposable mp3 = (IDisposable)ctor.Invoke(new object[] { filename }))
                {
                    PropertyInfo durationProp = mpegFileType.GetProperty("Duration");
                    if (durationProp == null)
                        return null;

                    TimeSpan duration = (TimeSpan)durationProp.GetValue(mp3);
                    return duration.TotalSeconds > 0 ? duration.TotalSeconds : (double?)null;
                }
            }
            catch
            {
                return null;
            }
        }

        private static double? GetMp4DurationViaDurationMine(string filename)
        {
            Assembly durationMineAssembly = FindLoadedOrLocalAssembly("Duration.Mine.Mp4");
            if (durationMineAssembly == null)
                return null;

            try
            {
                Type mp4DurationType = durationMineAssembly.GetType("Duration.Mine.Mp4.Mp4Duration");
                if (mp4DurationType == null)
                    return null;

                MethodInfo method = mp4DurationType.GetMethod("GetMp4Duration", new[] { typeof(string) });
                if (method == null)
                    return null;

                object result = method.Invoke(null, new object[] { filename });
                if (result == null)
                    return null;

                double seconds = Convert.ToDouble(result);
                return seconds > 0 ? seconds : (double?)null;
            }
            catch
            {
                return null;
            }
        }

        private static Assembly FindLoadedOrLocalAssembly(string name)
        {
            try
            {
                // Check already loaded assemblies
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return asm;
                }

                // Try loading from the same directory as the executing assembly
                string ownDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (!string.IsNullOrEmpty(ownDir))
                {
                    string candidate = Path.Combine(ownDir, name + ".dll");
                    if (File.Exists(candidate))
                        return Assembly.LoadFrom(candidate);
                }

                // Try the current working directory
                string cwdCandidate = Path.Combine(Environment.CurrentDirectory, name + ".dll");
                if (File.Exists(cwdCandidate))
                    return Assembly.LoadFrom(cwdCandidate);
            }
            catch
            {
                return null;
            }

            return null;
        }

        private static double? GetDurationViaFfprobe(string filename)
        {
            string ffprobe = FindExecutable("ffprobe");
            if (string.IsNullOrEmpty(ffprobe))
                return null;

            var psi = new ProcessStartInfo
            {
                FileName = ffprobe,
                Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filename}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                    return null;

                output = output.Trim();
                if (double.TryParse(output, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double seconds))
                    return seconds > 0 ? seconds : (double?)null;
            }

            return null;
        }

        private static string FindExecutable(string name)
        {
            string[] extensions = Environment.OSVersion.Platform == PlatformID.Win32NT
                ? new[] { ".exe", ".cmd", ".bat" }
                : new[] { "" };

            string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            var paths = pathEnv.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var dir in paths)
            {
                foreach (var ext in extensions)
                {
                    string candidate = Path.Combine(dir.Trim(), name + ext);
                    if (File.Exists(candidate))
                        return candidate;
                }
            }

            return null;
        }

        private static double? GetDurationViaShell(string filename)
        {
            // Windows-only Shell property system
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return null;

            try
            {
                var shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType == null)
                    return null;

                object shell = Activator.CreateInstance(shellType);
                string folderPath = Path.GetDirectoryName(filename);
                string fileName = Path.GetFileName(filename);

                var namespaceMethod = shellType.GetMethod("Namespace", new[] { typeof(object) });
                if (namespaceMethod == null)
                    return null;

                object folder = namespaceMethod.Invoke(shell, new object[] { folderPath });
                if (folder == null)
                    return null;

                var folderType = folder.GetType();
                var parseNameMethod = folderType.GetMethod("ParseName", new[] { typeof(string) });
                if (parseNameMethod == null)
                    return null;

                object folderItem = parseNameMethod.Invoke(folder, new object[] { fileName });
                if (folderItem == null)
                    return null;

                // Property 27 is "Duration" in the Windows shell namespace.
                var getDetailsMethod = folderType.GetMethod("GetDetailsOf", new[] { typeof(object), typeof(int) });
                if (getDetailsMethod == null)
                    return null;

                string durationString = getDetailsMethod.Invoke(folder, new object[] { folderItem, 27 }) as string;
                return ParseWindowsDuration(durationString);
            }
            catch
            {
                return null;
            }
        }

        private static double? ParseWindowsDuration(string durationString)
        {
            if (string.IsNullOrWhiteSpace(durationString))
                return null;

            durationString = durationString.Trim();
            string[] parts = durationString.Split(':');
            if (parts.Length == 3 &&
                int.TryParse(parts[0], out int hours) &&
                int.TryParse(parts[1], out int minutes) &&
                double.TryParse(parts[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double seconds))
            {
                double total = hours * 3600 + minutes * 60 + seconds;
                return total > 0 ? total : (double?)null;
            }

            return null;
        }
    }
}
