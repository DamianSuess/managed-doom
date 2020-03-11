﻿using System;
using System.Linq;

namespace ManagedDoom
{
    public sealed partial class World
    {
        public GameOptions Options;
        public Player[] Players;

        private Map map;
        private DoomRandom random;

        private int consoleplayer = 0;

        private int validCount;

        private int totalKills = 0;
        private int totalItems = 0;

        public int levelTime = 0;

        public World(Resources resorces, string mapName, GameOptions options, Player[] players)
        {
            Options = options;
            Players = players;

            map = new Map(resorces, mapName);

            random = new DoomRandom();

            validCount = 0;

            InitThinkers();
            InitThingMovement();
            InitPathTraversal();
            InitSight();

            LoadThings();
        }

        public void Update()
        {
            for (var i = 0; i < Player.MaxPlayerCount; i++)
            {
                if (Players[i].InGame)
                {
                    PlayerThink(Players[i]);
                }
            }

            RunThinkers();

            levelTime++;
        }

        private void LoadThings()
        {
            totalKills = 0;
            totalItems = 0;

            for (var i = 0; i < map.Things.Length; i++)
            {
                var mt = map.Things[i];

                var spawn = true;

                // Do not spawn cool, new monsters if !commercial
                if (Options.GameMode != GameMode.Commercial)
                {
                    switch (mt.Type)
                    {
                        case 68:    // Arachnotron
                        case 64:    // Archvile
                        case 88:    // Boss Brain
                        case 89:    // Boss Shooter
                        case 69:    // Hell Knight
                        case 67:    // Mancubus
                        case 71:    // Pain Elemental
                        case 65:    // Former Human Commando
                        case 66:    // Revenant
                        case 84:    // Wolf SS
                            spawn = false;
                            break;
                    }
                }

                if (spawn == false)
                {
                    break;
                }

                SpawnMapThing(mt);
            }
        }

        private void SpawnMapThing(Thing mthing)
        {
            /*
            // count deathmatch start positions
            if (mthing->type == 11)
            {
                if (deathmatch_p < &deathmatchstarts[10])
                {
                    memcpy(deathmatch_p, mthing, sizeof(*mthing));
                    deathmatch_p++;
                }
                return;
            }
            */

            // check for players specially
            if ((int)mthing.Type <= 4)
            {
                // save spots for respawning in network games
                //playerstarts[mthing->type - 1] = *mthing;
                if (Options.Deathmatch == 0)
                {
                    SpawnPlayer(mthing);
                }

                return;
            }

            // The code below must be removed later
            // when the player related code above is correctly implemented.
            if (mthing.Type == 11 || mthing.Type <= 4)
            {
                return;
            }

            // check for apropriate skill level
            if (!Options.NetGame && ((int)mthing.Flags & 16) != 0)
            {
                return;
            }

            int bit;
            if (Options.GameSkill == Skill.Baby)
            {
                bit = 1;
            }
            else if (Options.GameSkill == Skill.Nightmare)
            {
                bit = 4;
            }
            else
            {
                bit = 1 << ((int)Options.GameSkill - 1);
            }

            if (((int)mthing.Flags & bit) == 0)
            {
                return;
            }

            int i;
            // find which type to spawn
            for (i = 0; i < Info.MobjInfos.Length; i++)
            {
                if (mthing.Type == Info.MobjInfos[i].DoomEdNum)
                {
                    break;
                }
            }

            if (i == Info.MobjInfos.Length)
            {
                throw new Exception("P_SpawnMapThing: Unknown type!");
            }

            // don't spawn keycards and players in deathmatch
            if (Options.Deathmatch != 0
                && (Info.MobjInfos[i].Flags & MobjFlags.NotDeathmatch) != 0)
            {
                return;
            }

            // don't spawn any monsters if -nomonsters
            if (Options.NoMonsters
                && (i == (int)MobjType.Skull
                    || (Info.MobjInfos[i].Flags & MobjFlags.CountKill) != 0))
            {
                return;
            }

            // spawn it
            Fixed x = mthing.X;
            Fixed y = mthing.Y;
            Fixed z;
            if ((Info.MobjInfos[i].Flags & MobjFlags.SpawnCeiling) != 0)
            {
                z = Mobj.OnCeilingZ;
            }
            else
            {
                z = Mobj.OnFloorZ;
            }

            var mobj = SpawnMobj(x, y, z, (MobjType)i);

            mobj.SpawnPoint = mthing;

            if (mobj.Tics > 0)
            {
                mobj.Tics = 1 + (random.Next() % mobj.Tics);
            }

            if ((mobj.Flags & MobjFlags.CountKill) != 0)
            {
                totalKills++;
            }

            if ((mobj.Flags & MobjFlags.CountItem) != 0)
            {
                totalItems++;
            }

            // mobj->angle = ANG45 * (mthing->angle/45);
            mobj.Angle = new Angle(Angle.Ang45.Data * (uint)(mthing.Angle / 45));

            if ((mthing.Flags & ThingFlags.Ambush) != 0)
            {
                mobj.Flags |= MobjFlags.Ambush;
            }
        }



        //
        // P_SpawnPlayer
        // Called when a player is spawned on the level.
        // Most of the player structure stays unchanged
        //  between levels.
        //
        private void SpawnPlayer(Thing mthing)
        {
            // not playing?
            if (!Players[(int)mthing.Type - 1].InGame)
            {
                return;
            }

            var p = Players[(int)mthing.Type - 1];

            if (p.PlayerState == PlayerState.Reborn)
            {
                Players[(int)mthing.Type - 1].Reborn();
            }

            var x = mthing.X;
            var y = mthing.Y;
            var z = Mobj.OnFloorZ;
            var mobj = SpawnMobj(x, y, z, MobjType.Player);

            // set color translations for player sprites
            if ((int)mthing.Type > 1)
            {
                //mobj->flags |= (mthing->type - 1) << MF_TRANSSHIFT;
            }
            mobj.Angle = new Angle(Angle.Ang45.Data * (uint)(mthing.Angle / 45));
            mobj.Player = p;
            mobj.Health = p.Health;

            p.Mobj = mobj;
            p.PlayerState = PlayerState.Live;
            p.Refire = 0;
            p.Message = null;
            p.DamageCount = 0;
            p.BonusCount = 0;
            p.ExtraLight = 0;
            p.FixedColorMap = 0;
            p.ViewHeight = Player.VIEWHEIGHT;

            // setup gun psprite
            SetupPsprites(p);

            // give all cards in death match mode
            if (Options.Deathmatch != 0)
            {
                for (var i = 0; i < (int)CardType.Count; i++)
                {
                    p.Cards[i] = true;
                }
            }

            /*
            if (mthing->type - 1 == consoleplayer)
            {
                // wake up the status bar
                ST_Start();
                // wake up the heads up text
                HU_Start();
            }
            */
        }

        public Mobj SpawnMobj(Fixed x, Fixed y, Fixed z, MobjType type)
        {
            var mobj = thinkerPool.RentMobj();

            var info = Info.MobjInfos[(int)type];

            mobj.Type = type;
            mobj.Info = info;
            mobj.X = x;
            mobj.Y = y;
            mobj.Radius = info.Radius;
            mobj.Height = info.Height;
            mobj.Flags = info.Flags;
            mobj.Health = info.SpawnHealth;

            if (Options.GameSkill != Skill.Nightmare)
            {
                mobj.ReactionTime = info.ReactionTime;
            }

            mobj.LastLook = random.Next() % Player.MaxPlayerCount;

            // do not set the state with P_SetMobjState,
            // because action routines can not be called yet
            var st = Info.States[(int)info.SpawnState];

            mobj.State = st;
            mobj.Tics = st.Tics;
            mobj.Sprite = st.Sprite;
            mobj.Frame = st.Frame;

            // set subsector and/or block links
            SetThingPosition(mobj);

            mobj.FloorZ = mobj.Subsector.Sector.FloorHeight;
            mobj.CeilingZ = mobj.Subsector.Sector.CeilingHeight;

            if (z == Mobj.OnFloorZ)
            {
                mobj.Z = mobj.FloorZ;
            }
            else if (z == Mobj.OnCeilingZ)
            {
                mobj.Z = mobj.CeilingZ - mobj.Info.Height;
            }
            else
            {
                mobj.Z = z;
            }

            //mobj->thinker.function.acp1 = (actionf_p1)P_MobjThinker;

            AddThinker(mobj);

            return mobj;
        }

        public void RemoveMobj(Mobj mobj)
        {
            if ((mobj.Flags & MobjFlags.Special) != 0
                && (mobj.Flags & MobjFlags.Dropped) == 0
                && (mobj.Type != MobjType.Inv)
                && (mobj.Type != MobjType.Ins))
            {
                //itemrespawnque[iquehead] = mobj->spawnpoint;
                //itemrespawntime[iquehead] = leveltime;
                //iquehead = (iquehead + 1) & (ITEMQUESIZE - 1);

                // lose one off the end?
                //if (iquehead == iquetail)
                //    iquetail = (iquetail + 1) & (ITEMQUESIZE - 1);
            }

            // unlink from sector and block lists
            UnsetThingPosition(mobj);

            // stop any playing sound
            StopSound(mobj);

            // free block
            RemoveThinker(mobj);
        }

        public bool SetMobjState(Mobj mobj, State state)
        {
            StateDef st;

            do
            {
                if (state == State.Null)
                {
                    mobj.State = Info.States[(int)State.Null];
                    RemoveMobj(mobj);
                    return false;
                }

                st = Info.States[(int)state];
                mobj.State = st;
                mobj.Tics = st.Tics;
                mobj.Sprite = st.Sprite;
                mobj.Frame = st.Frame;

                // Modified handling.
                // Call action functions when the state is set
                if (st.MobjAction != null)
                {
                    st.MobjAction(mobj);
                }

                state = st.Next;
            }
            while (mobj.Tics == 0);

            return true;
        }



        //
        // P_CheckMissileSpawn
        // Moves the missile forward a bit
        //  and possibly explodes it right there.
        //
        public void P_CheckMissileSpawn(Mobj th)
        {
            th.Tics -= random.Next() & 3;
            if (th.Tics < 1)
            {
                th.Tics = 1;
            }

            // move a little forward so an angle can
            // be computed if it immediately explodes
            th.X += new Fixed(th.MomX.Data >> 1);
            th.Y += new Fixed(th.MomY.Data >> 1);
            th.Z += new Fixed(th.MomZ.Data >> 1);

            if (!P_TryMove(th, th.X, th.Y))
            {
                P_ExplodeMissile(th);
            }
        }


        //
        // P_SpawnMissile
        //
        public Mobj SpawnMissile(Mobj source, Mobj dest, MobjType type)
        {
            var th = SpawnMobj(
                source.X,
                source.Y,
                source.Z + new Fixed(4 * 8 * Fixed.FracUnit), type);

            if (th.Info.SeeSound != 0)
            {
                StartSound(th, th.Info.SeeSound);
            }

            // where it came from
            th.Target = source;

            var an = Geometry.PointToAngle(
                source.X,
                source.Y,
                dest.X,
                dest.Y);

            // fuzzy player
            if ((dest.Flags & MobjFlags.Shadow) != 0)
            {
                an += new Angle((random.Next() - random.Next()) << 20);
            }

            th.Angle = an;
            //an >>= ANGLETOFINESHIFT;
            th.MomX = new Fixed(th.Info.Speed) * Trig.Cos(an); // finecosine[an]);
            th.MomY = new Fixed(th.Info.Speed) * Trig.Sin(an); // finesine[an]);

            var dist = Geometry.AproxDistance(dest.X - source.X, dest.Y - source.Y);
            dist = dist / new Fixed(th.Info.Speed);

            if (dist.Data < 1)
            {
                dist = new Fixed(1);
            }

            th.MomZ = (dest.Z - source.Z) / dist;

            P_CheckMissileSpawn(th);

            return th;
        }

        public void StartSound(Mobj mobj, Sfx sfx)
        {
            Console.WriteLine("StartSound: " + sfx);
        }

        public void StopSound(Mobj mobj)
        {
        }

        public int GetNewValidCount()
        {
            validCount++;
            return validCount;
        }

        public int GetMobjHash()
        {
            var hash = 0;
            var current = thinkerCap.Next;
            while (current != thinkerCap)
            {
                if (!current.Removed)
                {
                    var mobj = current as Mobj;
                    if (mobj != null)
                    {
                        hash = DoomDebug.CombineHash(hash, mobj.GetHashCode());
                    }
                }
                current = current.Next;
            }
            return hash;
        }

        public Map Map => map;
        public DoomRandom Random => random;
    }
}