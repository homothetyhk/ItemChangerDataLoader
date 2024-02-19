using ItemChanger;
using MenuChanger;
using RandomizerMod.Logging;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using System.Reflection;
using System.Security.Cryptography;
using ICSettings = ItemChanger.Settings;
using static RandomizerMod.Localization;

namespace ItemChangerDataLoader
{
    public partial class ICDLMenu
    {
        public class StartData
        {
            public ICPack Pack { get; init; }
            public ICSettings Settings { get; init; }
            public RandoModContext? CTX { get; init; }
            public string? WarningMessage { get; init; }
            public string? ErrorMessage { get; init; }

            /// <summary>
            /// Computes a deterministic hash from the pack json.
            /// </summary>
            /// <returns>int32 hash value</returns>
            public int Hash()
            {
                using FileStream fs = new(Path.Combine(Pack._directory, "ic.json"), FileMode.Open, FileAccess.Read);
                using SHA256Managed sha256 = new();
                byte[] bytes = sha256.ComputeHash(fs);
                int seed = 17;
                for (int i = 0; i < bytes.Length; i++) seed = 31 * seed ^ bytes[i];

                return seed;
            }

            /// <summary>
            /// Applies the ICSettings to the save. If the pack supports rando tracking, also creates rando save data and applies the CTX.
            /// </summary>
            public void ApplySettings()
            {
                if (CTX is null)
                {
                    ItemChangerMod.CreateSettingsProfile(Settings);
                    return;
                }
                else
                {
                    typeof(RandomizerMod.RandomizerMod)
                        .GetProperty(nameof(RandomizerMod.RandomizerMod.RS), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        .SetValue(null, new RandomizerSettings
                        {
                            GenerationSettings = CTX.GenerationSettings,
                            Context = CTX,
                            ProfileID = GameManager.instance.profileID,
                            TrackerData = new() { AllowSequenceBreaks = true, logFileName = "TrackerDataDebugHistory.txt", },
                            TrackerDataWithoutSequenceBreaks = new() { AllowSequenceBreaks = false, logFileName = "TrackerDataWithoutSequenceBreaksDebugHistory.txt", }
                        });

                    ItemChangerMod.CreateSettingsProfile(Settings);

                    typeof(LogManager)
                        .GetMethod("WriteLogs", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        .Invoke(null, new object[]
                        {
                            new LogArguments
                            {
                                ctx = CTX,
                                gs = CTX.GenerationSettings,
                                randomizer = null
                            }
                        });

                    LogManager.Write(tw => JsonUtil.SerializeCTX(tw, CTX), "RawSpoiler.json");
                    RandomizerMod.RandomizerMod.RS.TrackerData.Setup(CTX.GenerationSettings, CTX);
                    RandomizerMod.RandomizerMod.RS.TrackerDataWithoutSequenceBreaks.Setup(CTX.GenerationSettings, CTX);
                    ((MenuChangerMod)typeof(MenuChangerMod).GetProperty("instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                        .GetValue(null))
                        .Settings.resumeKey = "Randomizer";
                    return;
                }
            }

            public static StartData Load(ICPack pack)
            {
                ICSettings s;
                RandoModContext? ctx = null;
                string? warning = null;
                string? error = null;

                try
                {
                    s = JsonUtil.Deserialize<ICSettings>(Path.Combine(pack._directory, "ic.json"));
                }
                catch (Exception e)
                {
                    s = null;
                    ICDLMod.Instance.LogError($"Error deserializing ic.json for pack {pack.Name}:\n{e}");

                    if (TryExtractMissingAssemblyError(e, out string assembly))
                    {
                        error = Localize("Error loading ItemChanger data from ic.json.") + "\n" + assembly + Localize(" may be missing or have the wrong version.");
                    }
                    else
                    {
                        error = Localize("Error loading ItemChanger data from ic.json.") + "\n" + Localize("See ModLog for details.");
                    }
                }
                if (pack.SupportsRandoTracking && error is null)
                {
                    try
                    {
                        ctx = JsonUtil.DeserializeCTX(Path.Combine(pack._directory, "ctx.json"));
                    }
                    catch (Exception e)
                    {
                        ICDLMod.Instance.LogError($"Error deserializing ctx.json for pack {pack.Name}:\n{e}");
                        if (TryExtractMissingAssemblyError(e, out string assembly))
                        {
                            warning = Localize($"Error deserializing ctx.json for pack {pack.Name}.") +
                                "\n" + assembly + Localize(" may be missing or have the wrong version.")
                                + "\n" + Localize("Logic tracking has been disabled.");
                        }
                        else
                        {
                            warning = Localize($"Error deserializing ctx.json for pack {pack.Name}.") +
                                "\n" + Localize("See ModLog for details.")
                            + "\n" + Localize("Logic tracking has been disabled.");
                        }
                    }
                }

                if (ctx is null) // remove RM modules if present to avoid error messages from null settings
                {
                    s.mods.Remove<RandomizerMod.IC.RandomizerModule>();
                    s.mods.Remove<RandomizerMod.IC.TrackerUpdate>();
                    s.mods.Remove<RandomizerMod.IC.HelperLogModule>();
                    s.mods.Remove<RandomizerMod.IC.TrackerLogModule>();
                }

                return new()
                {
                    Pack = pack,
                    Settings = s,
                    CTX = ctx,
                    WarningMessage = warning,
                    ErrorMessage = error,
                };
            }

            private static bool TryExtractMissingAssemblyError(Exception e, out string missingAssembly)
            {
                try
                {
                    string message = e.Message;
                    const string prefix = "Error resolving type specified in JSON ";

                    if (message.StartsWith(prefix))
                    {
                        missingAssembly = message.Split('\'')[1].Split(',')[1].Trim();
                        return true;
                    }
                }
                catch { }
                missingAssembly = null;
                return false;
            }
        }
    }
}
