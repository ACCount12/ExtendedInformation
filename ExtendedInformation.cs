using System;
using System.Reflection;
using System.Collections.Generic;
using Harmony;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using HBS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace ModNamespace
{

    // Display mech tonnage in battle UI
    [HarmonyPatch(typeof(BattleTech.UI.CombatHUDActorDetailsDisplay), "RefreshInfo")]
    public static class Patch_BattleTech_UI_CombatHUDActorDetailsDisplay_RefreshInfo
    {
        static float GetActorTonnage(AbstractActor actor)
        {
            if (actor is Vehicle vehicle)
                return vehicle.VehicleDef.Chassis.Tonnage;
            else if (actor is Mech mech)
                return mech.MechDef.Chassis.Tonnage;

            return 0;
        }

        static void Postfix(CombatHUDActorDetailsDisplay __instance)
        {
            int tonnage = (int)GetActorTonnage(__instance.DisplayedActor);

            if (tonnage > 0)
            {
                TextMeshProUGUI text_field = __instance.ActorWeightText;
                text_field.text = string.Format("{0} TONS \n{1}", tonnage, text_field.text);

                // HAX! Using this dot to increase line count by one and effectively move visible text field up.
                // This stops tonnage text from overlapping with jumpjet text.
                // The dot itself is not displayed because jumpjet text below it overlaps it.
                if (__instance.DisplayedActor.WorkingJumpjets != 0)
                    text_field.text = string.Format("{0}\n .", text_field.text);

            }
        }
    }

    // Display min and max salvage as "prioity salvage / salvage" on negotiation panel
    [HarmonyPatch(typeof(BattleTech.UI.SGContractsWidget), "PopulateContract")]
    public static class Patch_BattleTech_UI_SGContractsWidget_PopulateContract
    {
        static void Postfix(SGContractsWidget __instance, Contract contract)
        {
            SimGameState Sim = (SimGameState)ReflectionHelper.Helper.GetPrivateProperty(__instance, "Sim");
            
            int MinSalvage = Sim.Constants.Finances.ContractFloorSalvageBonus;
            int MaxSalvage = Sim.Constants.Salvage.DefaultSalvagePotential;

            if (contract.Override.salvagePotential > -1)
                MaxSalvage = contract.Override.salvagePotential;
            else if (contract.SalvagePotential > -1)
                MaxSalvage = contract.SalvagePotential;

            if (MaxSalvage > 0)
                MaxSalvage = MaxSalvage + MinSalvage;
            
            TextMeshProUGUI NegSalvageMin = (TextMeshProUGUI)ReflectionHelper.Helper.GetPrivateField(__instance, "NegSalvageMin");
            TextMeshProUGUI NegSalvageMax = (TextMeshProUGUI)ReflectionHelper.Helper.GetPrivateField(__instance, "NegSalvageMax");
            

            NegSalvageMin.text = string.Format("{0} / {1}", Math.Floor(MinSalvage / 4f), MinSalvage);
            NegSalvageMax.text = string.Format("{0} / {1}", Math.Floor(MaxSalvage / 4f), MaxSalvage);
        }
    }

    [HarmonyPatch(typeof(BattleTech.UI.MechLabHardpointElement), "SetData")]
    public static class Patch_BattleTech_UI_MechLabHardpointElement_SetData
    {
        static bool Prefix(ref WeaponCategory weaponCategory, string text)
        {
            if (text == "0/0" || text == "0")
                weaponCategory = WeaponCategory.NotSet;
            return true;
        }
    }

    [HarmonyPatch(typeof(BattleTech.UI.MechLabMechInfoWidget), "RefreshInfo")]
    public static class Patch_BattleTech_UI_MechLabMechInfoWidget_RefreshInfo
    {
        static void Postfix(MechLabMechInfoWidget __instance)
        {
            MechLabHardpointElement[] hardpoints = (MechLabHardpointElement[])ReflectionHelper.Helper.GetPrivateField(__instance, "hardpoints");
            MechLabPanel mechLab = (MechLabPanel)ReflectionHelper.Helper.GetPrivateField(__instance, "mechLab");

            var all_locations = new List<MechLabLocationWidget>
            {
                mechLab.headWidget, mechLab.centerTorsoWidget,
                mechLab.leftTorsoWidget, mechLab.rightTorsoWidget,
                mechLab.leftArmWidget, mechLab.rightArmWidget,
                mechLab.leftLegWidget, mechLab.rightLegWidget
            };

            int totalBallisticHardpoints = 0;
            int totalEnergyHardpoints = 0;
            int totalMissileHardpoints = 0;
            int totalSmallHardpoints = 0;

            int currentBallisticCount = 0;
            int currentEnergyCount = 0;
            int currentMissileCount = 0;
            int currentSmallCount = 0;
            
            foreach (var location in all_locations)
            {
                totalBallisticHardpoints += location.totalBallisticHardpoints;
                totalEnergyHardpoints += location.totalEnergyHardpoints;
                totalMissileHardpoints += location.totalMissileHardpoints;
                totalSmallHardpoints += location.totalSmallHardpoints;

                currentBallisticCount += (int)ReflectionHelper.Helper.GetPrivateField(location, "currentBallisticCount");
                currentEnergyCount += (int)ReflectionHelper.Helper.GetPrivateField(location, "currentEnergyCount");
                currentMissileCount += (int)ReflectionHelper.Helper.GetPrivateField(location, "currentMissileCount");
                currentSmallCount += (int)ReflectionHelper.Helper.GetPrivateField(location, "currentSmallCount");
            };

            hardpoints[0].SetData(WeaponCategory.Ballistic, string.Format("{0}/{1}", currentBallisticCount, totalBallisticHardpoints));
            hardpoints[1].SetData(WeaponCategory.Energy, string.Format("{0}/{1}", currentEnergyCount, totalEnergyHardpoints));
            hardpoints[2].SetData(WeaponCategory.Missile, string.Format("{0}/{1}", currentMissileCount, totalMissileHardpoints));
            hardpoints[3].SetData(WeaponCategory.AntiPersonnel, string.Format("{0}/{1}", currentSmallCount, totalSmallHardpoints));

        }
    }



    [HarmonyPatch(typeof(BattleTech.UI.MechBayMechInfoWidget), "SetDescriptions")]
    public static class Patch_BattleTech_UI_MechBayMechInfoWidget_SetDescriptions
    {
        static void Postfix(MechBayMechInfoWidget __instance)
        {
            ModBase.Sim = __instance.sim;
            if (ModBase.combatConstants == null)
                ModBase.combatConstants = CombatGameConstants.CreateFromSaved(UnityGameInstance.BattleTechGame);
        }
    }


    /*
    [HarmonyPatch(typeof(BattleTech.StatTooltipData), "SetData")]
    public static class Patch_BattleTech_StatTooltipData_SetData
    {
        static void Postfix(MechDef def, BattleTech.StatTooltipData __instance)
        {
            //
        }
    }
    */

    [HarmonyPatch(typeof(BattleTech.StatTooltipData), "SetHeatData")]
    public static class Patch_BattleTech_StatTooltipData_SetHeatData
    {
        static bool Prefix()
        {
            return false;
        }

        static void Postfix(MechDef def, BattleTech.StatTooltipData __instance)
        {
            HeatConstantsDef heatConstants = ModBase.combatConstants.Heat;

            float total_heat_sinking = heatConstants.InternalHeatSinkCount * heatConstants.DefaultHeatSinkDissipationCapacity;
            float extra_engine_heat_sinking = BTMechDef.GetExtraEngineSinking(def);
            total_heat_sinking += extra_engine_heat_sinking;

            float heat_sinking_ratio = 1;
            float total_weapon_heat = 0;
            float weapon_heat_ratio = 1;

            float jump_distance = BTMechDef.GetJumpJetsMaxDistance(def);

            int max_heat = heatConstants.MaxHeat;

            
            foreach (MechComponentRef mechComponentRef in def.Inventory)
            {
                if (mechComponentRef.Def == null)
                    mechComponentRef.RefreshComponentDef();

                // Weapon total heat
                if (mechComponentRef.Def is WeaponDef weapon)
                    total_weapon_heat += (float)weapon.HeatGenerated;

                // Heat sink total dissipation
                else if (mechComponentRef.Def is HeatSinkDef heat_sink)
                    total_heat_sinking += heat_sink.DissipationCapacity;

                // Bank/Exchanger effects 
                if (mechComponentRef.Def.statusEffects != null)
                    foreach (EffectData effect in mechComponentRef.Def.statusEffects)
                    {
                        StatisticEffectData statisticData = effect.statisticData;
                        if (statisticData.statName == "MaxHeat")
                            BTStatistics.ApplyEffectStatistic(statisticData, ref max_heat);
                        else if (statisticData.statName == "HeatGenerated" && 
                            (statisticData.targetCollection == StatisticEffectData.TargetCollection.Weapon || BTStatistics.IsWildcardStatistic(statisticData)))
                            BTStatistics.ApplyEffectStatistic(statisticData, ref weapon_heat_ratio);
                        else if (statisticData.statName == "JumpDistanceMultiplier")
                            BTStatistics.ApplyEffectStatistic(statisticData, ref jump_distance);
                    }
            }

            total_weapon_heat *= weapon_heat_ratio;
            total_heat_sinking *= heat_sinking_ratio;

            if (extra_engine_heat_sinking > 0f)
                __instance.dataList.Add("Heat dissipation", string.Format("{0}\n(DHS)", (int)total_heat_sinking));
            else
                __instance.dataList.Add("Heat dissipation", string.Format("{0}", (int)total_heat_sinking));

            if (total_weapon_heat > 0)
            {
                __instance.dataList.Add("Alpha strike heat", string.Format("{0}", (int)total_weapon_heat));
                __instance.dataList.Add("Alpha strike heat delta", string.Format("{0}", (int)(total_weapon_heat - total_heat_sinking)));
            }

            __instance.dataList.Add("Max heat capacity", string.Format("{0}", (int)max_heat));

            if (jump_distance > 0)
            {
                float max_jump_heat = ((jump_distance / heatConstants.JumpHeatUnitSize) + 1) * heatConstants.JumpHeatPerUnit;
                max_jump_heat *= heatConstants.GlobalHeatIncreaseMultiplier;
                max_jump_heat = Mathf.Max(heatConstants.JumpHeatMin, max_jump_heat);

                __instance.dataList.Add("Max jump heat", string.Format("{0}", (int)max_jump_heat));
            }
        }
    }

    [HarmonyPatch(typeof(BattleTech.StatTooltipData), "SetMeleeData")]
    public static class Patch_BattleTech_StatTooltipData_SetMeleeData
    {
        static bool Prefix()
        {
            return false;
        }

        static void Postfix(MechDef def, BattleTech.StatTooltipData __instance)
        {
            ChassisDef currentChassis = def.Chassis;

            float melee_damage = currentChassis.MeleeDamage;
            float melee_instability = currentChassis.MeleeInstability;

            float dfa_damage = currentChassis.DFADamage * 2;
            float dfa_instability = currentChassis.DFAInstability;
            float dfa_self_damage = currentChassis.DFASelfDamage;

            float support_damage = 0;
            float support_instability = 0;
            float support_heat = 0;

            foreach (MechComponentRef mechComponentRef in def.Inventory)
            {
                if (mechComponentRef.Def == null)
                    mechComponentRef.RefreshComponentDef();

                // Take Melee/DFA upgrades into account
                if (mechComponentRef.Def.statusEffects != null)
                    foreach (EffectData effect in mechComponentRef.Def.statusEffects)
                    {
                        if (effect.effectType != EffectType.StatisticEffect)
                            continue;

                        if (effect.statisticData.targetWeaponSubType == WeaponSubType.Melee)
                        {
                            if (effect.statisticData.statName == "DamagePerShot")
                                BTStatistics.ApplyEffectStatistic(effect.statisticData, ref melee_damage);
                            else if (effect.statisticData.statName == "Instability")
                                BTStatistics.ApplyEffectStatistic(effect.statisticData, ref melee_instability);
                        }
                        else if (effect.statisticData.targetWeaponSubType == WeaponSubType.DFA)
                        {
                            if (effect.statisticData.statName == "DamagePerShot")
                                BTStatistics.ApplyEffectStatistic(effect.statisticData, ref dfa_damage);
                            else if (effect.statisticData.statName == "Instability")
                                BTStatistics.ApplyEffectStatistic(effect.statisticData, ref dfa_instability);
                            else if (effect.statisticData.statName == "DFASelfDamage")
                                BTStatistics.ApplyEffectStatistic(effect.statisticData, ref dfa_self_damage);
                        }
                    }

                // Calculate support weapon damage
                if (mechComponentRef.Def is WeaponDef weapon && weapon.Category == WeaponCategory.AntiPersonnel)
                {
                    support_damage += weapon.Damage * weapon.ShotsWhenFired;
                    support_instability += weapon.Instability * weapon.ShotsWhenFired;
                    support_heat += weapon.HeatDamage * weapon.ShotsWhenFired;
                }

            }

            __instance.dataList.Add("Melee damage", ModBase.MakeDamageString(melee_damage, melee_instability));

            if (BTMechDef.GetJumpJetsAmount(def) > 0)
            {
                __instance.dataList.Add("DFA damage", ModBase.MakeDamageString(dfa_damage, dfa_instability));
                __instance.dataList.Add("DFA self-damage", string.Format("{0}x2", (int)dfa_self_damage));
            }

            string damage_string = ModBase.MakeDamageString(support_damage, support_instability, support_heat);
            if (!String.IsNullOrEmpty(damage_string))
                __instance.dataList.Add("Support damage", damage_string);

        }
    }

    [HarmonyPatch(typeof(BattleTech.StatTooltipData), "SetFirepowerData")]
    public static class Patch_BattleTech_StatTooltipData_SetFirepowerData
    {
        static bool Prefix()
        {
            return false;
        }

        static void Postfix(MechDef def, BattleTech.StatTooltipData __instance)
        {
            float alpha_damage = 0;
            float alpha_instability = 0;
            float alpha_heat = 0;

            // Calculate alpha strike total damage, heat damage and instability.
            foreach (MechComponentRef mechComponentRef in def.Inventory)
            {
                if (mechComponentRef.Def is WeaponDef weapon)
                {
                    alpha_damage += weapon.Damage * weapon.ShotsWhenFired;
                    alpha_instability += weapon.Instability * weapon.ShotsWhenFired;
                    alpha_heat += weapon.HeatDamage * weapon.ShotsWhenFired;
                }

            }

            string damage_string = ModBase.MakeDamageString(alpha_damage, alpha_instability, alpha_heat);
            if (!String.IsNullOrEmpty(damage_string))
                __instance.dataList.Add("Alpha strike damage", damage_string);

        }
    }

    [HarmonyPatch(typeof(BattleTech.StatTooltipData), "SetMovementData")]
    public static class Patch_BattleTech_StatTooltipData_SetMovementData
    {
        static bool Prefix()
        {
            return false;
        }

        static void Postfix(MechDef def, BattleTech.StatTooltipData __instance)
        {
            ChassisDef currentChassis = def.Chassis;

            float max_walk_distance = currentChassis.MovementCapDef.MaxWalkDistance;
            float max_sprint_distance = currentChassis.MovementCapDef.MaxSprintDistance;
            float max_jump_distance = BTMechDef.GetJumpJetsMaxDistance(def);

            foreach (MechComponentRef mechComponentRef in def.Inventory)
            {
                if (mechComponentRef.Def == null)
                    mechComponentRef.RefreshComponentDef();

                // Various movement effects 
                if (mechComponentRef.Def.statusEffects != null)
                    foreach (EffectData effect in mechComponentRef.Def.statusEffects)
                    {
                        StatisticEffectData statisticData = effect.statisticData;
                        if (statisticData.statName == "WalkSpeed")
                            BTStatistics.ApplyEffectStatistic(statisticData, ref max_walk_distance);
                        else if (statisticData.statName == "RunSpeed")
                            BTStatistics.ApplyEffectStatistic(statisticData, ref max_sprint_distance);
                        else if (statisticData.statName == "JumpDistanceMultiplier")
                            BTStatistics.ApplyEffectStatistic(statisticData, ref max_jump_distance);
                    }
            }

            __instance.dataList.Add("Walk distance", string.Format("{0}m", (int)max_walk_distance));
            __instance.dataList.Add("Sprint distance", string.Format("{0}m", (int)max_sprint_distance));

            if (max_jump_distance > 0)
                __instance.dataList.Add("Jump distance", string.Format("{0}m", (int)max_jump_distance));

        }
    }


    public static class BTStatistics
    {
        public static void ApplyEffectStatistic(StatisticEffectData effect, ref float cur_value)
        {
            float parsed_float = float.Parse(effect.modValue);

            switch (effect.operation)
            {
                case StatCollection.StatOperation.Float_Add:
                    cur_value += parsed_float;
                    break;
                case StatCollection.StatOperation.Float_Subtract:
                    cur_value -= parsed_float;
                    break;
                case StatCollection.StatOperation.Float_Multiply:
                case StatCollection.StatOperation.Float_Multiply_Int:
                    cur_value *= parsed_float;
                    break;
                case StatCollection.StatOperation.Float_Divide:
                case StatCollection.StatOperation.Float_Divide_Int:
                    cur_value /= parsed_float;
                    break;
                case StatCollection.StatOperation.Float_Divide_Denom:
                case StatCollection.StatOperation.Float_Divide_Denom_Int:
                    cur_value = parsed_float / cur_value;
                    break;
            }
        }

        public static void ApplyEffectStatistic(StatisticEffectData effect, ref int cur_value)
        {
            float parsed_float = float.Parse(effect.modValue);
            int parsed_int = (int)parsed_float;

            switch (effect.operation)
            {
                case StatCollection.StatOperation.Int_Add:
                    cur_value += parsed_int;
                    break;
                case StatCollection.StatOperation.Int_Subtract:
                    cur_value -= parsed_int;
                    break;
                case StatCollection.StatOperation.Int_Multiply:
                    cur_value *= parsed_int;
                    break;
                case StatCollection.StatOperation.Int_Multiply_Float:
                    cur_value = (int)((float)cur_value * parsed_float);
                    break;
                case StatCollection.StatOperation.Int_Divide:
                    cur_value /= parsed_int;
                    break;
                case StatCollection.StatOperation.Int_Divide_Float:
                    cur_value = (int)((float)cur_value / parsed_float);
                    break;
                case StatCollection.StatOperation.Int_Divide_Denom:
                    cur_value = parsed_int / cur_value;
                    break;
                case StatCollection.StatOperation.Int_Divide_Denom_Float:
                    cur_value = (int)(parsed_float / (float)cur_value);
                    break;
                case StatCollection.StatOperation.Int_Mod:
                    cur_value %= parsed_int;
                    break;
            }

        }

        public static bool IsWildcardStatistic(StatisticEffectData effect)
        {
            return 
                (effect.targetCollection == StatisticEffectData.TargetCollection.NotSet) && 
                (effect.targetWeaponSubType == WeaponSubType.NotSet) && 
                (effect.additionalRules == EffectType.NotSet) &&
                (effect.targetWeaponCategory == WeaponCategory.NotSet) &&
                (effect.targetWeaponType == WeaponType.NotSet) &&
                (effect.targetAmmoCategory == AmmoCategory.NotSet);
        }
    }

    public static class BTMechDef
    {
        public static int GetJumpJetsAmount(MechDef mech)
        {
            int amount = 0;

            foreach (MechComponentRef mechComponentRef in mech.Inventory)
            {
                if (mechComponentRef.ComponentDefType == ComponentType.JumpJet)
                    amount++;
            }

            return amount;
        }

        public static float GetJumpJetsMaxDistance(MechDef mech)
        {
            int jumpjets_amt = GetJumpJetsAmount(mech);

            if (jumpjets_amt <= 0)
                return 0;

            float jump_distance = 0;
            if (jumpjets_amt >= ModBase.combatConstants.MoveConstants.MoveTable.Length)
                jump_distance = ModBase.combatConstants.MoveConstants.MoveTable[ModBase.combatConstants.MoveConstants.MoveTable.Length - 1];
            else
                jump_distance = ModBase.combatConstants.MoveConstants.MoveTable[jumpjets_amt];

            return jump_distance;

        }

        // Checks if InternalHeaters mod is installed. Returns extra engine heat sinking capability, if any.
        private static bool _internalHeatersLoadedChecked = false;
        private static Assembly _internalHeatersAssembly = null;
        private const string InternalHeatersAssemblyIndentifier = "InternalHeaters, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

        public static float GetExtraEngineSinking(MechDef mech)
        {
            if (!_internalHeatersLoadedChecked)
            {
                _internalHeatersLoadedChecked = true;

                try
                {
                    _internalHeatersAssembly = Assembly.Load(InternalHeatersAssemblyIndentifier);
                    var method = _internalHeatersAssembly.GetType("InternalHeaters.Calculators").GetMethod("DoubleHeatsinkEngineDissipation");
                    method.Invoke(null, new object[] { mech, ModBase.combatConstants.Heat });
                }
                catch (Exception)
                {
                    // If InternalHeaters mod isn't present or isn't functional, just ignore it.
                    _internalHeatersAssembly = null;
                }
            }

            // If InternalHeaters mod is loaded, let it handle the calculations.
            if (_internalHeatersAssembly != null)
            {
                var method = _internalHeatersAssembly.GetType("InternalHeaters.Calculators").GetMethod("DoubleHeatsinkEngineDissipation");
                return (float)method.Invoke(null, new object[] { mech, ModBase.combatConstants.Heat });
            }

            return 0;
        }

    }

    public static class ModBase
    {
        public static string modName = "ExtendedInformation";
        public static CombatGameConstants combatConstants;
        public static SimGameState Sim;
        public static Dictionary<string, string> dataList = null;

        public static void Init()
        {
            var harmony = HarmonyInstance.Create("org.null.ACCount." + modName);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static string MakeDamageString(float f_damage, float f_stability = 0, float f_heat = 0)
        {
            int damage = (int)f_damage;
            int stability = (int)f_stability;
            int heat = (int)f_heat;

            if (heat > 0 && stability > 0)
                return string.Format("{0}\n{1} S\n{2} H", damage, stability, heat);
            else if (stability > 0)
                return string.Format("{0}\n{1} S", damage, stability);
            else if (heat > 0)
                return string.Format("{0}\n{1} H", damage, heat);
            else if (damage > 0)
                return string.Format("{0}", damage);

            return String.Empty;
        }

    }
}
