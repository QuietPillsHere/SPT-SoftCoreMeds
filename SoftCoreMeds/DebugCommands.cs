using Comfort.Common;
using EFT;
using EFT.Console.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SoftCoreMeds
{
    /// <summary>
    /// stolen code from Lacyway to help debuging
    /// </summary>
    public abstract class DebugCommands
    {
        [ConsoleCommand("damageLimbs")]
        public static void DamageLimbs()
        {
            if (Singleton<GameWorld>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                if (gameWorld.MainPlayer != null)
                {
                    gameWorld.MainPlayer.ActiveHealthController.ApplyDamage(EBodyPart.Stomach, 75 , default);
                    gameWorld.MainPlayer.ActiveHealthController.ApplyDamage(EBodyPart.LeftArm, 75 , default);
                    gameWorld.MainPlayer.ActiveHealthController.ApplyDamage(EBodyPart.RightArm, 75, default);
                    gameWorld.MainPlayer.ActiveHealthController.ApplyDamage(EBodyPart.LeftLeg, 75, default);
                    gameWorld.MainPlayer.ActiveHealthController.ApplyDamage(EBodyPart.RightLeg, 75, default);
                }
            }
        }

        [ConsoleCommand("damageChest")]
        public static void DamageChest()
        {
            if (Singleton<GameWorld>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                if (gameWorld.MainPlayer != null)
                {
                    gameWorld.MainPlayer.ActiveHealthController.ApplyDamage(EBodyPart.Chest, 100, default);
                }
            }
        }

        [ConsoleCommand("restoreLimbs")]
        public static void RestoreLimbs()
        {
            if (Singleton<GameWorld>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                if (gameWorld.MainPlayer != null)
                {
                    gameWorld.MainPlayer.ActiveHealthController.RestoreFullHealth();
                }
            }
        }
    }

}
