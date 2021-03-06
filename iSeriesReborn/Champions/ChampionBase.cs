﻿using System;
using System.Collections.Generic;
using iSeriesReborn.Utility;
using iSeriesReborn.Utility.ModuleHelper;
using LeagueSharp;
using LeagueSharp.Common;

namespace iSeriesReborn.Champions
{
    abstract class ChampionBase
    {
        private delegate void OrbwalkerDelegate();

        private Dictionary<Orbwalking.OrbwalkingMode, OrbwalkerDelegate> OrbwalkerCallbacks;

        public void OnLoad()
        {
            OrbwalkerCallbacks = new Dictionary<Orbwalking.OrbwalkingMode, OrbwalkerDelegate>()
            {
                { Orbwalking.OrbwalkingMode.Combo, OnCombo },
                { Orbwalking.OrbwalkingMode.Mixed, OnMixed },
                { Orbwalking.OrbwalkingMode.LastHit, OnLastHit },
                { Orbwalking.OrbwalkingMode.LaneClear, OnLaneClear },
                { Orbwalking.OrbwalkingMode.None, () => { } }
            };

            OnChampLoad();
            LoadMenu();
            Game.OnUpdate += OnUpdate;
            Obj_AI_Base.OnDoCast += AfterAttack;
        }

        private void OnUpdate(EventArgs args)
        {
            if (OrbwalkerCallbacks.ContainsKey(Variables.Orbwalker.ActiveMode))
            {
                OrbwalkerCallbacks[Variables.Orbwalker.ActiveMode]();
            }

            foreach (var module in GetModules())
            {
                if (module.ShouldRun() && module.GetModuleType() == ModuleType.OnUpdate)
                {
                    module.Run();
                }
            }

            OnTick();
        }


        private void AfterAttack(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && Orbwalking.IsAutoAttack(args.SData.Name))
            {
                foreach (var module in GetModules())
                {
                    if (module.ShouldRun() && module.GetModuleType() == ModuleType.OnAfterAA)
                    {
                        module.Run();
                    }
                }
            }
        }

        protected abstract void OnChampLoad();

        protected abstract void LoadMenu();

        protected abstract void OnTick();

        protected abstract void OnCombo();

        protected abstract void OnMixed();

        protected abstract void OnLastHit();

        protected abstract void OnLaneClear();

        public abstract Dictionary<SpellSlot, Spell> GetSpells();

        public abstract List<IModule> GetModules();
    }
}
