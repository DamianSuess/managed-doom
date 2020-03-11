﻿using System;

namespace ManagedDoom
{
    public sealed class GameOptions
    {
        public Skill GameSkill = Skill.Hard;
        public GameMode GameMode = GameMode.Commercial;
        public bool NetGame = false;
        public int Episode = 1;
        public int Map = 1;
        public int Deathmatch = 0;
        public bool RespawnMonsters = false;
        public bool FastMonsters = false;
        public bool NoMonsters = false;
        public int ConsolePlayer = 0;
    }
}