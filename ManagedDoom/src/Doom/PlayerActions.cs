﻿using System;

namespace ManagedDoom
{
    public static class PlayerActions
    {
        public static void Light0(Player player, PlayerSpriteDef psp)
        {
        }

        public static void WeaponReady(Player player, PlayerSpriteDef psp)
        {
            var world = player.Mobj.World;

            // get out of attack state
            if (player.Mobj.State == Info.States[(int)State.PlayAtk1]
                || player.Mobj.State == Info.States[(int)State.PlayAtk2])
            {
                world.SetMobjState(player.Mobj, State.Play);
            }

            if (player.ReadyWeapon == WeaponType.Chainsaw
                && psp.State == Info.States[(int)State.Saw])
            {
                world.StartSound(player.Mobj, Sfx.SAWIDL);
            }

            // check for change
            //  if player is dead, put the weapon away
            if (player.PendingWeapon != WeaponType.NoChange || player.Health == 0)
            {
                // change weapon
                //  (pending weapon should allready be validated)
                var newstate = Info.WeaponInfos[(int)player.ReadyWeapon].DownState;
                world.P_SetPsprite(player, PlayerSprite.Weapon, newstate);
                return;
            }

            // check for fire
            //  the missile launcher and bfg do not auto fire
            if ((player.Cmd.Buttons & Buttons.Attack) != 0)
            {
                if (!player.AttackDown
                    || (player.ReadyWeapon != WeaponType.Missile
                        && player.ReadyWeapon != WeaponType.Bfg))
                {
                    player.AttackDown = true;
                    FireWeapon(player);
                    return;
                }
            }
            else
            {
                player.AttackDown = false;
            }

            // bob the weapon based on movement speed
            var angle = (128 * player.Mobj.World.levelTime) & Trig.FineMask;
            psp.Sx = Fixed.One + player.Bob * Trig.Cos(angle);
            angle &= Trig.FineAngleCount / 2 - 1;
            psp.Sy = World.WEAPONTOP + player.Bob * Trig.Sin(angle);
        }





        //
        // P_FireWeapon.
        //
        private static void FireWeapon(Player player)
        {
            var world = player.Mobj.World;

            /*
            if (!P_CheckAmmo(player))
            {
                return;
            }
            */

            world.SetMobjState(player.Mobj, State.PlayAtk1);
            var newstate = Info.WeaponInfos[(int)player.ReadyWeapon].AttackState;
            world.P_SetPsprite(player, PlayerSprite.Weapon, newstate);
            //P_NoiseAlert(player->mo, player->mo);
        }








        public static void Lower(Player player, PlayerSpriteDef psp)
        {
        }

        public static void Raise(Player player, PlayerSpriteDef psp)
        {
            psp.Sy -= World.RAISESPEED;

            if (psp.Sy > World.WEAPONTOP)
            {
                return;
            }

            psp.Sy = World.WEAPONTOP;

            // The weapon has been raised all the way,
            //  so change to the ready state.
            var newstate = Info.WeaponInfos[(int)player.ReadyWeapon].ReadyState;

            player.Mobj.World.P_SetPsprite(player, PlayerSprite.Weapon, newstate);
        }

        public static void Punch(Player player, PlayerSpriteDef psp)
        {
        }

        public static void ReFire(Player player, PlayerSpriteDef psp)
        {
            // check for fire
            //  (if a weaponchange is pending, let it go through instead)
            if ((player.Cmd.Buttons & Buttons.Attack) != 0
                && player.PendingWeapon == WeaponType.NoChange
                && player.Health != 0)
            {
                player.Refire++;
                FireWeapon(player);
            }
            else
            {
                player.Refire = 0;
                //P_CheckAmmo(player);
            }
        }

        private static void P_BulletSlope(Mobj mo)
        {
            var world = mo.World;

            // see which target is to be aimed at
            var an = mo.Angle;
            world.bulletslope = world.AimLineAttack(mo, an, new Fixed(16 * 64 * Fixed.FracUnit));

            if (world.linetarget == null)
            {
                an += new Angle(1 << 26);
                world.bulletslope = world.AimLineAttack(mo, an, new Fixed(16 * 64 * Fixed.FracUnit));
                if (world.linetarget == null)
                {
                    an -= new Angle(2 << 26);
                    world.bulletslope = world.AimLineAttack(mo, an, new Fixed(16 * 64 * Fixed.FracUnit));
                }
            }
        }

        //
        // P_GunShot
        //
        private static void P_GunShot(Mobj mo, bool accurate)
        {
            var world = mo.World;

            var damage = 5 * (world.Random.Next() % 3 + 1);
            var angle = mo.Angle;

            if (!accurate)
            {
                angle += new Angle((world.Random.Next() - world.Random.Next()) << 18);
            }

            world.LineAttack(mo, angle, World.MISSILERANGE, world.bulletslope, damage);
        }


        public static void FirePistol(Player player, PlayerSpriteDef psp)
        {
            var world = player.Mobj.World;

            world.StartSound(player.Mobj, Sfx.PISTOL);

            world.SetMobjState(player.Mobj, State.PlayAtk2);
            player.Ammo[(int)Info.WeaponInfos[(int)player.ReadyWeapon].Ammo]--;

            world.P_SetPsprite(player,
                PlayerSprite.Flash,
                Info.WeaponInfos[(int)player.ReadyWeapon].FlashState);

            P_BulletSlope(player.Mobj);
            P_GunShot(player.Mobj, player.Refire == 0);
        }

        public static void Light1(Player player, PlayerSpriteDef psp)
        {
        }

        public static void FireShotgun(Player player, PlayerSpriteDef psp)
        {
        }

        public static void Light2(Player player, PlayerSpriteDef psp)
        {
        }

        public static void FireShotgun2(Player player, PlayerSpriteDef psp)
        {
        }

        public static void CheckReload(Player player, PlayerSpriteDef psp)
        {
        }

        public static void OpenShotgun2(Player player, PlayerSpriteDef psp)
        {
        }

        public static void LoadShotgun2(Player player, PlayerSpriteDef psp)
        {
        }

        public static void CloseShotgun2(Player player, PlayerSpriteDef psp)
        {
        }

        public static void FireCGun(Player player, PlayerSpriteDef psp)
        {
        }

        public static void GunFlash(Player player, PlayerSpriteDef psp)
        {
        }

        public static void FireMissile(Player player, PlayerSpriteDef psp)
        {
        }

        public static void Saw(Player player, PlayerSpriteDef psp)
        {
        }

        public static void FirePlasma(Player player, PlayerSpriteDef psp)
        {
        }

        public static void BFGsound(Player player, PlayerSpriteDef psp)
        {
        }

        public static void FireBFG(Player player, PlayerSpriteDef psp)
        {
        }
    }
}
