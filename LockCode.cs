using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Lock Code", "ThornFox", "1.1.0")]
    [Description("Shows admins the code, guest code and authorized players of a lock. | Zeigt Admins den Code, Gastcode und die berechtigten Spieler eines Schlosses an.")]
    public class LockCode : RustPlugin
    {
        #region Fields

        private const string PermUse = "lockcode.use";
        private const BindingFlags MemberFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly Regex RichTextTag = new Regex("<[^>]*>", RegexOptions.Compiled);

        private PluginConfig _config;

        #endregion

        #region Configuration

        private class PluginConfig
        {
            [JsonProperty("Config version")]
            public string ConfigVersion = "1.1.0";

            [JsonProperty("Max look distance (m)")]
            public float MaxDistance = 10f;

            [JsonProperty("Admins can use without permission")]
            public bool AdminBypass = true;

            [JsonProperty("Log usage to file")]
            public bool LogUsage = true;

            [JsonProperty("Max listed players per list")]
            public int MaxListedPlayers = 15;
        }

        protected override void LoadDefaultConfig() => _config = new PluginConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<PluginConfig>();
                if (_config == null)
                    throw new Exception("configuration is empty");

                if (_config.ConfigVersion != Version.ToString())
                {
                    MigrateConfig();
                    SaveConfig();
                }
            }
            catch (Exception ex)
            {
                PrintWarning($"Configuration is invalid, loading defaults ({ex.Message}).");
                LoadDefaultConfig();
                SaveConfig();
            }
        }

        private void MigrateConfig()
        {
            // Versions before 1.1.0 had no configuration file.
            // Missing keys are filled with the defaults of PluginConfig; invalid values are reset here.
            if (_config.MaxDistance <= 0f)
                _config.MaxDistance = 10f;

            if (_config.MaxListedPlayers <= 0)
                _config.MaxListedPlayers = 15;

            _config.ConfigVersion = Version.ToString();
            PrintWarning($"Configuration migrated to version {Version}.");
        }

        protected override void SaveConfig() => Config.WriteObject(_config, true);

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You don't have permission to use this command.",
                ["NothingInSight"] = "No object in sight (max. {0} m).",
                ["NoLockFound"] = "No lock found (hit: <color=#ffa500>{0}</color>). Look directly at a door, box or lock.",
                ["LockOn"] = "<color=#ffa500>Lock on:</color> {0}",
                ["Owner"] = "<color=#ffa500>Owner:</color> {0}",
                ["Locked"] = "<color=#ffa500>Locked:</color> {0}",
                ["Code"] = "<color=#ffa500>Code:</color> {0}",
                ["GuestCode"] = "<color=#ffa500>Guest code:</color> {0}",
                ["Authorized"] = "<color=#ffa500>Authorized:</color> {0}",
                ["Guests"] = "<color=#ffa500>Guests:</color> {0}",
                ["KeyLock"] = "<color=#ffa500>Key lock, key code:</color> {0}",
                ["VehicleLock"] = "<color=#ffa500>Type:</color> vehicle code lock",
                ["VehicleLockUnreadable"] = "Vehicle lock found, but its details cannot be read on this Rust version.",
                ["Yes"] = "yes",
                ["No"] = "no",
                ["NoCodeSet"] = "no code set",
                ["None"] = "none",
                ["Nobody"] = "nobody",
                ["Unknown"] = "unknown",
                ["AndMore"] = "+{0} more"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "Du hast keine Berechtigung für diesen Befehl.",
                ["NothingInSight"] = "Kein Objekt im Blickfeld (max. {0} m).",
                ["NoLockFound"] = "Kein Schloss gefunden (getroffen: <color=#ffa500>{0}</color>). Schau direkt auf Tür, Kiste oder Schloss.",
                ["LockOn"] = "<color=#ffa500>Schloss an:</color> {0}",
                ["Owner"] = "<color=#ffa500>Besitzer:</color> {0}",
                ["Locked"] = "<color=#ffa500>Abgeschlossen:</color> {0}",
                ["Code"] = "<color=#ffa500>Code:</color> {0}",
                ["GuestCode"] = "<color=#ffa500>Gastcode:</color> {0}",
                ["Authorized"] = "<color=#ffa500>Berechtigt:</color> {0}",
                ["Guests"] = "<color=#ffa500>Gäste:</color> {0}",
                ["KeyLock"] = "<color=#ffa500>Schlüsselschloss, Key-Code:</color> {0}",
                ["VehicleLock"] = "<color=#ffa500>Typ:</color> Fahrzeug-Codeschloss",
                ["VehicleLockUnreadable"] = "Fahrzeugschloss gefunden, die Details sind in dieser Rust-Version aber nicht lesbar.",
                ["Yes"] = "ja",
                ["No"] = "nein",
                ["NoCodeSet"] = "kein Code gesetzt",
                ["None"] = "keiner",
                ["Nobody"] = "niemand",
                ["Unknown"] = "unbekannt",
                ["AndMore"] = "+{0} weitere"
            }, this, "de");
        }

        private string Lang(string key, BasePlayer player, params object[] args)
        {
            var message = lang.GetMessage(key, this, player?.UserIDString);
            return args.Length > 0 ? string.Format(message, args) : message;
        }

        private void Reply(BasePlayer player, string key, params object[] args) =>
            SendReply(player, Lang(key, player, args));

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(PermUse, this);
        }

        #endregion

        #region Commands

        // /lockcode - look at a door, box, vehicle or directly at the lock
        [ChatCommand("lockcode")]
        private void CmdLockCode(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;

            if (!CanUse(player))
            {
                Reply(player, "NoPermission");
                return;
            }

            // Check every hit along the view direction (rugs, decorations etc. are skipped),
            // ignore triggers and take the closest object that carries a lock.
            var hits = Physics.RaycastAll(player.eyes.HeadRay(), _config.MaxDistance, -1, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
            {
                Reply(player, "NothingInSight", _config.MaxDistance);
                return;
            }

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            BaseEntity firstEntity = null;

            foreach (var hit in hits)
            {
                var entity = hit.GetEntity();
                if (entity == null || entity == player)
                    continue;

                if (firstEntity == null)
                    firstEntity = entity;

                var baseLock = FindLock(entity);
                if (baseLock != null)
                {
                    ShowLockInfo(player, baseLock);
                    return;
                }

                var car = FindModularCar(entity);
                if (car != null && TryShowCarLockInfo(player, car))
                    return;
            }

            if (firstEntity != null)
                Reply(player, "NoLockFound", firstEntity.ShortPrefabName);
            else
                Reply(player, "NothingInSight", _config.MaxDistance);
        }

        #endregion

        #region Lock output

        private void ShowLockInfo(BasePlayer player, BaseLock baseLock)
        {
            var sb = new StringBuilder();
            var target = baseLock.GetParentEntity();

            sb.AppendLine(Lang("LockOn", player, target != null ? target.ShortPrefabName : Lang("Unknown", player)));
            sb.AppendLine(Lang("Owner", player, GetName(baseLock.OwnerID, player)));
            sb.Append(Lang("Locked", player, Lang(baseLock.IsLocked() ? "Yes" : "No", player)));

            var codeLock = baseLock as CodeLock;
            if (codeLock != null)
            {
                sb.AppendLine();
                sb.AppendLine(Lang("Code", player, codeLock.hasCode ? codeLock.code : Lang("NoCodeSet", player)));
                sb.AppendLine(Lang("GuestCode", player, codeLock.hasGuestCode ? codeLock.guestCode : Lang("None", player)));
                sb.AppendLine(Lang("Authorized", player, GetNames(codeLock.whitelistPlayers, player)));
                sb.Append(Lang("Guests", player, GetNames(codeLock.guestPlayers, player)));
            }
            else
            {
                var keyLock = baseLock as KeyLock;
                if (keyLock != null)
                {
                    sb.AppendLine();
                    sb.Append(Lang("KeyLock", player, keyLock.keyCode));
                }
            }

            SendReply(player, sb.ToString());
            LogUsage(player, baseLock.OwnerID, baseLock.transform.position);
        }

        // Modular cars use their own lock object (ModularCarCodeLock) instead of a BaseLock entity.
        // Its members are read via reflection so that a Rust update renaming them cannot break compilation.
        private bool TryShowCarLockInfo(BasePlayer player, ModularCar car)
        {
            var carLock = GetMemberValue(car, "CarLock", "carLock");
            if (carLock == null)
                return false;

            var hasLock = GetMemberValue(carLock, "HasALock", "hasALock") as bool?;
            if (hasLock.HasValue && !hasLock.Value)
                return false;

            var code = GetMemberValue(carLock, "Code", "code") as string;
            var whitelist = GetMemberValue(carLock, "WhitelistPlayers", "whitelistPlayers") as List<ulong>;

            if (code == null && whitelist == null)
            {
                if (!hasLock.HasValue)
                    return false;

                Reply(player, "VehicleLockUnreadable");
                LogUsage(player, car.OwnerID, car.transform.position);
                return true;
            }

            var sb = new StringBuilder();
            sb.AppendLine(Lang("LockOn", player, car.ShortPrefabName));
            sb.AppendLine(Lang("VehicleLock", player));
            sb.AppendLine(Lang("Owner", player, GetName(car.OwnerID, player)));
            sb.AppendLine(Lang("Code", player, string.IsNullOrEmpty(code) ? Lang("NoCodeSet", player) : code));
            sb.Append(Lang("Authorized", player, GetNames(whitelist, player)));

            SendReply(player, sb.ToString());
            LogUsage(player, car.OwnerID, car.transform.position);
            return true;
        }

        #endregion

        #region Helpers

        private bool CanUse(BasePlayer player) =>
            (_config.AdminBypass && player.IsAdmin) || permission.UserHasPermission(player.UserIDString, PermUse);

        private static BaseLock FindLock(BaseEntity entity)
        {
            var directLock = entity as BaseLock;
            if (directLock != null)
                return directLock;

            var slotLock = entity.GetSlot(BaseEntity.Slot.Lock) as BaseLock;
            if (slotLock != null)
                return slotLock;

            // e.g. parts whose parent entity carries the lock
            var parent = entity.GetParentEntity();
            if (parent != null)
            {
                var parentLock = parent as BaseLock;
                if (parentLock != null)
                    return parentLock;

                var parentSlotLock = parent.GetSlot(BaseEntity.Slot.Lock) as BaseLock;
                if (parentSlotLock != null)
                    return parentSlotLock;
            }

            return null;
        }

        private static ModularCar FindModularCar(BaseEntity entity)
        {
            // Hits usually land on a vehicle module; walk up the parent chain to the car.
            var current = entity;
            for (var depth = 0; current != null && depth < 4; depth++)
            {
                var car = current as ModularCar;
                if (car != null)
                    return car;

                current = current.GetParentEntity();
            }

            return null;
        }

        private static object GetMemberValue(object source, params string[] names)
        {
            if (source == null)
                return null;

            var type = source.GetType();
            foreach (var name in names)
            {
                try
                {
                    var property = type.GetProperty(name, MemberFlags);
                    if (property != null && property.GetIndexParameters().Length == 0)
                        return property.GetValue(source, null);

                    var field = type.GetField(name, MemberFlags);
                    if (field != null)
                        return field.GetValue(source);
                }
                catch (Exception)
                {
                    // Member exists but cannot be read; try the next name.
                }
            }

            return null;
        }

        private string GetNames(List<ulong> ids, BasePlayer viewer)
        {
            if (ids == null || ids.Count == 0)
                return Lang("Nobody", viewer);

            var limit = Math.Max(1, _config.MaxListedPlayers);
            var names = new List<string>();

            for (var i = 0; i < ids.Count && i < limit; i++)
                names.Add(GetName(ids[i], viewer));

            var result = string.Join(", ", names.ToArray());
            if (ids.Count > limit)
                result += " " + Lang("AndMore", viewer, ids.Count - limit);

            return result;
        }

        private string GetName(ulong id, BasePlayer viewer)
        {
            if (id == 0)
                return Lang("Nobody", viewer);

            var p = covalence.Players.FindPlayerById(id.ToString());
            return p != null ? $"{StripRichText(p.Name)} ({id})" : id.ToString();
        }

        // Player names may contain rich text tags that would break the chat output.
        private static string StripRichText(string text) =>
            string.IsNullOrEmpty(text) ? text : RichTextTag.Replace(text, string.Empty);

        private void LogUsage(BasePlayer player, ulong ownerId, Vector3 position)
        {
            if (!_config.LogUsage)
                return;

            // The code itself is never written to the log.
            LogToFile("usage",
                $"{StripRichText(player.displayName)} ({player.UserIDString}) queried the lock of {GetName(ownerId, null)} at {position}.",
                this);
        }

        #endregion
    }
}
