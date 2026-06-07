using Surfer.Attributes;
using Surfer.Helpers;
using Hazel;
using UnityEngine;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class UpdateSystemHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.UpdateSystem;

    internal SystemTypes CatchedSystemType;

    private readonly Dictionary<uint, Func<PlayerControl?, ISystemType, MessageReader, byte, bool>> systemHandlers;

    private static SabotageSystemType? SabotageSystem => 
        ShipStatus.Instance != null && ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Sabotage, out var sys) 
        ? sys.Cast<SabotageSystemType>() 
        : null;

    internal UpdateSystemHandler()
    {
        systemHandlers = new Dictionary<uint, Func<PlayerControl?, ISystemType, MessageReader, byte, bool>>
        {
            { (uint)SystemTypes.Sabotage, (sender, system, reader, count) => { try { return HandleSabotageSystem(sender, system.Cast<SabotageSystemType>(), reader); } catch { return true; } } },
            { (uint)SystemTypes.Ventilation, (sender, system, reader, count) => { try { return HandleVentilationSystem(sender, system.Cast<VentilationSystem>(), count); } catch { return true; } } },
            { (uint)SystemTypes.Electrical, (sender, system, reader, count) => { try { return HandleSwitchSystem(sender, system.Cast<SwitchSystem>(), count); } catch { return true; } } },
            { (uint)SystemTypes.Comms, (sender, system, reader, count) => { try { return HandleCommsSystem(sender, system, count); } catch { return true; } } },
            { (uint)SystemTypes.MushroomMixupSabotage, (sender, system, reader, count) => { try { return HandleMushroomMixupSabotageSystem(sender, system.Cast<MushroomMixupSabotageSystem>(), count); } catch { return true; } } },
            { (uint)SystemTypes.Doors, (sender, system, reader, count) => { try { return HandleDoorsSystem(sender, system.Cast<DoorsSystemType>(), count); } catch { return true; } } },
            { (uint)SystemTypes.Reactor, (sender, system, reader, count) => { try { return HandleReactorSystem(sender, system.Cast<ReactorSystemType>(), count); } catch { return true; } } },
            { (uint)SystemTypes.Laboratory, (sender, system, reader, count) => { try { return HandleReactorSystem(sender, system.Cast<ReactorSystemType>(), count); } catch { return true; } } },
            { (uint)SystemTypes.HeliSabotage, (sender, system, reader, count) => { try { return HandleHeliSabotageSystem(sender, system.Cast<HeliSabotageSystem>(), count); } catch { return true; } } },
            { (uint)SystemTypes.LifeSupp, (sender, system, reader, count) => { try { return HandleLifeSuppSystem(sender, system.Cast<LifeSuppSystemType>(), count); } catch { return true; } } }
        };
    }

    internal static bool CheckConsoleDistance<T>(PlayerControl? player, float distance = 2f) where T : PlayerTask, new()
    {
        if (player == null) return false;

        var playerPos = player.GetCustomPosition();
        var consolesPos = new T().FindConsolesPos();

        foreach (var consolePos in consolesPos)
        {
            if (Vector2.Distance(consolePos, playerPos) < distance)
                return true;
        }

        return false;
    }

    internal override bool HandleAntiCheatCancel(PlayerControl? sender, MessageReader reader)
    {
        if (GameState.IsHost && sender.IsHost()) return true;

        byte count = reader.ReadByte();

        if (ShipStatus.Instance.Systems.TryGetValue(CatchedSystemType, out ISystemType system))
        {
            uint systemKey = (uint)CatchedSystemType;

            if (systemHandlers.TryGetValue(systemKey, out var handler))
            {
                return handler.Invoke(sender, system, reader, count);
            }
        }

        return true;
    }

    private static bool HandleSabotageSystem(PlayerControl? sender, SabotageSystemType sabotageSystem, MessageReader reader)
    {
        byte count = reader.ReadByte();

        if (!sender.IsImpostorTeam())
        {
            return false;
        }

        if (sabotageSystem.Timer > 0f)
        {
            return false;
        }

        return true;
    }

    private static bool HandleVentilationSystem(PlayerControl? sender, VentilationSystem ventilationSystem, byte count)
    {

        return true;
    }

    private static bool HandleSwitchSystem(PlayerControl? sender, SwitchSystem switchSystem, byte count)
    {
        if (count == 128) // Direct sabotage call from client, which is not possible, only the host should have this count when HandleSabotageSystem it's called
        {
            return false;
        }

        if (!switchSystem.IsActive)
        {
            return false;
        }

        if (!CheckConsoleDistance<ElectricTask>(sender))
        {
            return false;
        }

        return true;
    }

    private static bool HandleCommsSystem(PlayerControl? sender, ISystemType system, byte count)
    {
        if (system == null) return false;

        try
        {
            var hqHudSystem = system.Cast<HqHudSystemType>();
            return HandleHqHudSystem(sender, hqHudSystem, count);
        }
        catch (Exception ex)
        {
            Logger_.Error(ex, "UpdateSystemHandler.HandleCommsSystem.HqHud");
        }

        try
        {
            var hudOverrideSystem = system.Cast<HudOverrideSystemType>();
            return HandleHudOverrideSystem(sender, hudOverrideSystem, count);
        }
        catch (Exception ex)
        {
            Logger_.Error(ex, "UpdateSystemHandler.HandleCommsSystem.HudOverride");
        }

        return true;
    }

    private static bool HandleHqHudSystem(PlayerControl? sender, HqHudSystemType hqHudSystem, byte count)
    {
        if ((count & 128) > 0) // Direct sabotage call from client, which is not possible, only the host should have this count when HandleSabotageSystem it's called
        {
            return false;
        }

        if (!hqHudSystem.IsActive)
        {
            return false;
        }

        if (!CheckConsoleDistance<HqHudOverrideTask>(sender, 2f))
        {
            return false;
        }

        return true;
    }

    private static bool HandleHudOverrideSystem(PlayerControl? sender, HudOverrideSystemType hudOverrideSystem, byte count)
    {
        if (count == 128) // Direct sabotage call from client, which is not possible, only the host should have this count when HandleSabotageSystem it's called
        {
            return false;
        }

        if (!hudOverrideSystem.IsActive)
        {
            return false;
        }

        if (!CheckConsoleDistance<HudOverrideTask>(sender, 2f))
        {
            return false;
        }

        return true;
    }

    private static bool HandleMushroomMixupSabotageSystem(PlayerControl? sender, MushroomMixupSabotageSystem mushroomMixupSabotage, byte count)
    {
        if (count == 1) // Direct sabotage call from client, which is not possible, only the host should have this count when HandleSabotageSystem it's called
        {
            return false;
        }

        if (mushroomMixupSabotage.IsActive)
        {
            return false;
        }

        return true;
    }

    private static bool HandleDoorsSystem(PlayerControl? sender, DoorsSystemType doorsSystem, byte count)
    {
        if (count == 128) // Direct sabotage call from client, which is not possible, only the host should have this count when HandleSabotageSystem it's called
        {
            return false;
        }

        return true;
    }

    private static bool HandleReactorSystem(PlayerControl? sender, ReactorSystemType reactorSystem, byte count)
    {
        if (count == 128 || count == 16) // Direct sabotage call from client, which is not possible, only the host should have this count when HandleSabotageSystem it's called
        {
            return false;
        }

        if (!reactorSystem.IsActive)
        {
            return false;
        }

        if (count.HasAnyBit(64))
        {
            foreach (var tuple in reactorSystem.UserConsolePairs)
            {
                if (tuple.Item1 == sender.PlayerId)
                {
                    return false;
                }
            }
        }

        /*
          if (!CheckConsoleDistance<ReactorTask>(sender))
          {
              return false;
          }
         */

        return true;
    }

    private static bool HandleHeliSabotageSystem(PlayerControl? sender, HeliSabotageSystem heliSabotageSystem, byte count)
    {
        if (count == 128) // Direct sabotage call from client, which is not possible, only the host should have this count when HandleSabotageSystem it's called
        {
            return false;
        }

        if (!heliSabotageSystem.IsActive)
        {
            return false;
        }

        if (!CheckConsoleDistance<HeliCharlesTask>(sender))
        {
            return false;
        }

        return true;
    }

    private static bool HandleLifeSuppSystem(PlayerControl? sender, LifeSuppSystemType lifeSuppSystem, byte count)
    {
        if (count == 128) // Direct sabotage call from client, which is not possible, only the host should have this count when HandleSabotageSystem it's called
        {
            return false;
        }

        if (!lifeSuppSystem.IsActive)
        {
            return false;
        }

        /*
        if (!CheckConsoleDistance<NoOxyTask>(sender))
        {
            return false;
        }
        */

        return true;
    }
}