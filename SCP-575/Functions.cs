﻿using Smod2;
using Smod2.API;
using Smod2.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using MEC;
using System.Threading;
using UnityEngine;

namespace SCP575
{
    public class Functions
    {
        private readonly SCP575 plugin;
        public Functions(SCP575 plugin)
        {
            this.plugin = plugin;
        }

        public void RunBlackout()
        {
            plugin.Debug("Blackout Function has started");
            if ((plugin.timer && plugin.timed_lcz) || (plugin.toggle && plugin.toggle_lcz))
            {
                foreach (Room room in plugin.BlackoutRoom)
                {
                    room.FlickerLights();
                }
            }
            Generator079.generators[0].CallRpcOvercharge();

            if (plugin.keter)
            {
                plugin.coroutines.Add(Timing.RunCoroutine(Keter()));
            }
        }

        public IEnumerator<float> ToggledBlackout(float delay)
        {
            yield return Timing.WaitForSeconds(delay);
            while (plugin.toggle)
            {
                RunBlackout();
                yield return Timing.WaitForSeconds(8f);
            }
        }

        public IEnumerator<float> TimedBlackout(float delay)
        {
            plugin.Debug("Being Delayed");
            yield return Timing.WaitForSeconds(delay);
            while (plugin.Timed)
            {
                plugin.Debug("Announcing");
                if (plugin.announce && plugin.timed_lcz && plugin.Timed)
                {
                    PlayerManager.localPlayer.GetComponent<MTFRespawn>().CallRpcPlayCustomAnnouncement("FACILITY POWER SYSTEM FAILURE IN 3 . 2 . 1 .", false);
                }
                else
                {
                    PlayerManager.localPlayer.GetComponent<MTFRespawn>().CallRpcPlayCustomAnnouncement("HEAVY CONTAINMENT POWER SYSTEM FAILURE IN 3 . 2 . 1 .", false);
                }
                yield return Timing.WaitForSeconds(8.7f);

                float blackout_dur;
                if (plugin.random_events)
                {
                    blackout_dur = plugin.gen.Next(plugin.random_dur_min, plugin.random_dur_max);
                }
                else
                {
                    blackout_dur = plugin.durTime;
                }

                plugin.Debug("Flipping Bools1");
                plugin.timer = true;
                plugin.triggerkill = true;
                plugin.Debug(plugin.timer.ToString() + plugin.triggerkill.ToString());
                do
                {
                    plugin.Debug("Running Blackout");
                    RunBlackout();
                    yield return Timing.WaitForSeconds(11);
                } while ((blackout_dur -= 11) > 0);

                plugin.Debug("Announcing Disabled.");
                if (plugin.announce && plugin.timed_lcz && plugin.Timed)
                {
                    PlayerManager.localPlayer.GetComponent<MTFRespawn>().CallRpcPlayCustomAnnouncement("FACILITY POWER SYSTEM NOW OPERATIONAL", false);
                }
                else
                {
                    PlayerManager.localPlayer.GetComponent<MTFRespawn>().CallRpcPlayCustomAnnouncement("HEAVY CONTAINMENT POWER SYSTEM NOW OPERATIONAL", false);
                }
                yield return Timing.WaitForSeconds(8.7f);

                plugin.Debug("Flipping bools2");
                plugin.timer = false;
                plugin.triggerkill = false;
                plugin.Debug("Timer: " + plugin.timer);
                plugin.Debug("Waiting to re-execute..");

                if (plugin.random_events)
                {
                    yield return Timing.WaitForSeconds(plugin.gen.Next(plugin.random_min, plugin.random_max));
                }
                else
                {
                    yield return Timing.WaitForSeconds(plugin.waitTime);
                }
            }
        }
        public IEnumerator<float> Keter()
        {
            plugin.Debug("Keter function started.");
            List<Player> players = plugin.Server.GetPlayers();
            List<Player> keterlist = new List<Player>();

            foreach (Player player in players)
            {
                if (player.TeamRole.Team == Smod2.API.Team.SPECTATOR || player.TeamRole.Team == Smod2.API.Team.SCP) continue;
                yield return Timing.WaitForSeconds(0.1f);
                if (HasFlashlight(player)) continue;
                yield return Timing.WaitForSeconds(0.1f);
                if (!IsInDangerZone(player)) continue;

                keterlist.Add(player);
            }

            if (plugin.keterkill && keterlist.Count > 0)
            {
                for (int i = 0; i < plugin.keterkill_num; i++)
                {
                    if (keterlist.Count == 0) break;

                    Player ply = players[plugin.gen.Next(keterlist.Count)];

                    ply.Kill();
                    keterlist.Remove(ply);
                    ply.PersonalClearBroadcasts();
                    ply.PersonalBroadcast(10, "You were killed by SCP-575!", false);
                }
            }
            else if (!plugin.keterkill && keterlist.Count > 0)
            {
                foreach (Player player in keterlist)
                {
                    player.Damage(plugin.KeterDamage);
                    player.PersonalClearBroadcasts();
                    player.PersonalBroadcast(5, "You were damaged by SCP-575!", false);
                }
            }
        }

        public bool HasFlashlight(Player player) =>
            (player.GetCurrentItem().ItemType == ItemType.FLASHLIGHT || (player.GetGameObject() as GameObject).GetComponent<WeaponManager>().flashlightEnabled);

        public bool IsInDangerZone(Player player)
        {
            Vector loc = player.GetPosition();
            foreach (Room room in plugin.rooms.Where(p => Vector.Distance(loc, p.Position) <= 12f))
            {
                if (room.ZoneType == ZoneType.HCZ || (plugin.timer && plugin.timed_lcz && room.ZoneType == ZoneType.LCZ) || (plugin.toggle && plugin.toggle_lcz && room.ZoneType == ZoneType.LCZ))
                    return true;
            }
            return false;
        }
        public void Get079Rooms()
        {
            foreach (Room room in PluginManager.Manager.Server.Map.Get079InteractionRooms(Scp079InteractionType.CAMERA))
            {
                if (room.ZoneType == ZoneType.LCZ)
                {
                    plugin.BlackoutRoom.Add(room);
                }
                plugin.rooms.Add(room);
            }
        }
        public void ToggleBlackout()
        {
            plugin.toggle = !plugin.toggle;
            if (plugin.Timed)
            {
                plugin.timed_override = true;
                plugin.Timed = false;
            }
            else if (plugin.timed_override)
            {
                plugin.timed_override = false;
                plugin.Timed = true;
            }

            if (plugin.toggle)
            {
                if (plugin.announce)
                {
                    if (!plugin.toggle_lcz)
                    {
                        PlayerManager.localPlayer.GetComponent<MTFRespawn>().CallRpcPlayCustomAnnouncement("HEAVY CONTAINMENT POWER SYSTEM FAILURE IN 3. . 2. . 1. . ", false);
                    }
                    else if (plugin.toggle_lcz)
                    {
                        PlayerManager.localPlayer.GetComponent<MTFRespawn>().CallRpcPlayCustomAnnouncement("FACILITY POWER SYSTEM FAILURE IN 3. . 2. . 1. . ", false);
                    }
                }
                plugin.coroutines.Add(Timing.RunCoroutine(ToggledBlackout(8.7f)));
            }
        }

        public bool IsAllowed(ICommandSender sender)
        {
            Player player = (sender is Player) ? sender as Player : null;
            if (player != null)
            {
                List<string> roleList = (plugin.validRanks != null && plugin.validRanks.Length > 0) ? plugin.validRanks.Select(role => role.ToUpper()).ToList() : new List<string>();
                if (roleList != null && roleList.Count > 0 && (roleList.Contains(player.GetUserGroup().Name.ToUpper()) || roleList.Contains(player.GetRankName().ToUpper())))
                {
                    return true;
                }
                else if (roleList == null || roleList.Count == 0)
                    return true;
                else
                    return false;
            }
            return true;
        }

        public void EnableBlackouts()
        {
            plugin.Timed = true;
        }

        public void DisableBlackouts()
        {
            plugin.Timed = false;
        }

        public void EnableAnnounce()
        {
            plugin.announce = true;
        }

        public void DisableAnnounce()
        {
            plugin.announce = false;
        }
    }
}
