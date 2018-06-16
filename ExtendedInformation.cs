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


namespace ExtendedInformation
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
            SimGameState Sim = (SimGameState)ReflectionHelper.GetPrivateProperty(__instance, "Sim");
            
            int MinSalvage = Sim.Constants.Finances.ContractFloorSalvageBonus;
            int MaxSalvage = Sim.Constants.Salvage.DefaultSalvagePotential;

            if (contract.Override.salvagePotential > -1)
                MaxSalvage = contract.Override.salvagePotential;
            else if (contract.SalvagePotential > -1)
                MaxSalvage = contract.SalvagePotential;

            if (MaxSalvage > 0)
                MaxSalvage = MaxSalvage + MinSalvage;
            
            TextMeshProUGUI NegSalvageMin = (TextMeshProUGUI)ReflectionHelper.GetPrivateField(__instance, "NegSalvageMin");
            TextMeshProUGUI NegSalvageMax = (TextMeshProUGUI)ReflectionHelper.GetPrivateField(__instance, "NegSalvageMax");
            

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
            MechLabHardpointElement[] hardpoints = (MechLabHardpointElement[])ReflectionHelper.GetPrivateField(__instance, "hardpoints");
            MechLabPanel mechLab = (MechLabPanel)ReflectionHelper.GetPrivateField(__instance, "mechLab");

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

                currentBallisticCount += (int)ReflectionHelper.GetPrivateField(location, "currentBallisticCount");
                currentEnergyCount += (int)ReflectionHelper.GetPrivateField(location, "currentEnergyCount");
                currentMissileCount += (int)ReflectionHelper.GetPrivateField(location, "currentMissileCount");
                currentSmallCount += (int)ReflectionHelper.GetPrivateField(location, "currentSmallCount");
            };

            hardpoints[0].SetData(WeaponCategory.Ballistic, string.Format("{0}/{1}", currentBallisticCount, totalBallisticHardpoints));
            hardpoints[1].SetData(WeaponCategory.Energy, string.Format("{0}/{1}", currentEnergyCount, totalEnergyHardpoints));
            hardpoints[2].SetData(WeaponCategory.Missile, string.Format("{0}/{1}", currentMissileCount, totalMissileHardpoints));
            hardpoints[3].SetData(WeaponCategory.AntiPersonnel, string.Format("{0}/{1}", currentSmallCount, totalSmallHardpoints));

        }
    }

    [HarmonyPatch(typeof(BattleTech.UI.Tooltips.TooltipPrefab_Generic), "SetData")]
    public static class Patch_BattleTech_UI_Tooltips_TooltipPrefab_Generic_SetData
    {
        static void Postfix(object data, ref bool __result, BattleTech.UI.Tooltips.TooltipPrefab_Generic __instance)
        {
            if (!__result || ModBase.currentMech == null)
                return;

            if ((ModBase.Sim == null || ModBase.Sim.CurRoomState != DropshipLocation.MECH_BAY) && !ModBase.inMechLab)
                return;

            if (ModBase.combatConstants == null)
                ModBase.combatConstants = CombatGameConstants.CreateFromSaved(UnityGameInstance.BattleTechGame);

            MechDef currentMech = ModBase.currentMech;
            ChassisDef currentChassis = currentMech.Chassis;

            BaseDescriptionDef baseDescriptionDef = (BaseDescriptionDef)data;

            string extra_stats = string.Empty;

            switch (baseDescriptionDef.Id)
            {
                case "TooltipMechPerformanceFirepower":

                    float alpha_damage = 0;
                    float alpha_instability = 0;
                    float alpha_heat = 0;

                    // Calculate alpha strike total damage, heat damage and instability.
                    foreach (MechComponentRef mechComponentRef in currentMech.Inventory)
                    {
                        if (mechComponentRef.Def is WeaponDef weapon)
                        {
                            alpha_damage += weapon.Damage * weapon.ShotsWhenFired;
                            alpha_instability += weapon.Instability * weapon.ShotsWhenFired;
                            alpha_heat += weapon.HeatDamage * weapon.ShotsWhenFired;
                        }

                    }

                    if (alpha_damage > 0)
                        if(alpha_heat > 0)
                            extra_stats += string.Format("Alpha strike damage: <b>{0}</b> ( <b>{1} H</b> )\n", alpha_damage, alpha_heat);
                        else
                            extra_stats += string.Format("Alpha strike damage: <b>{0}</b>\n", alpha_damage);

                    if(alpha_instability > 0)
                        extra_stats += string.Format("Alpha strike stability damage: <b>{0}</b>\n", alpha_instability);

                    break;

                case "TooltipMechPerformanceHeat":
                    HeatConstantsDef heatConstants = ModBase.combatConstants.Heat;

                    float total_heat_sinking = heatConstants.InternalHeatSinkCount * heatConstants.DefaultHeatSinkDissipationCapacity;
                    float heat_sinking_ratio = 1;
                    float total_weapon_heat = 0;
                    float weapon_heat_ratio = 1;

                    float jump_distance = BTMechDef.GetJumpJetsMaxDistance(currentMech);

                    int max_heat = heatConstants.MaxHeat;

                    foreach (MechComponentRef mechComponentRef in currentMech.Inventory)
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
                                else if (statisticData.statName == "HeatGenerated" && statisticData.targetCollection == StatisticEffectData.TargetCollection.Weapon)
                                    BTStatistics.ApplyEffectStatistic(statisticData, ref weapon_heat_ratio);
                                else if (statisticData.statName == "JumpDistanceMultiplier")
                                    BTStatistics.ApplyEffectStatistic(statisticData, ref jump_distance);
                            }
                    }

                    total_weapon_heat *= weapon_heat_ratio;
                    total_heat_sinking *= heat_sinking_ratio;

                    extra_stats += string.Format("Heat dissipation: <b>{0}</b>\n", (int)total_heat_sinking);

                    if (total_weapon_heat > 0)
                    {
                        extra_stats += string.Format("Alpha strike heat: <b>{0}</b>\n", (int)total_weapon_heat);
                        extra_stats += string.Format("Alpha strike heat delta: <b>{0}</b>\n", (int)(total_weapon_heat - total_heat_sinking));
                    }

                    extra_stats += string.Format("Max heat capacity: <b>{0}</b>\n", (int)max_heat);

                    if (jump_distance > 0)
                    {
                        float max_jump_heat = ((jump_distance / heatConstants.JumpHeatUnitSize) + 1) * heatConstants.JumpHeatPerUnit;
                        max_jump_heat *= heatConstants.GlobalHeatIncreaseMultiplier;
                        max_jump_heat = Mathf.Max(heatConstants.JumpHeatMin, max_jump_heat);

                        extra_stats += string.Format("Max jump heat: <b>{0}</b>\n", (int)max_jump_heat);
                    }
                    break;

                case "TooltipMechPerformanceSpeed":
                    float max_walk_distance = currentChassis.MovementCapDef.MaxWalkDistance;
                    float max_sprint_distance = currentChassis.MovementCapDef.MaxSprintDistance;
                    float max_jump_distance = BTMechDef.GetJumpJetsMaxDistance(currentMech);

                    foreach (MechComponentRef mechComponentRef in currentMech.Inventory)
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

                    extra_stats += string.Format("Walk distance: <b>{0}m</b>\n", (int)max_walk_distance);
                    extra_stats += string.Format("Sprint distance: <b>{0}m</b>\n", (int)max_sprint_distance);

                    if (max_jump_distance > 0)
                        extra_stats += string.Format("Jump distance: <b>{0}m</b>\n", (int)max_jump_distance);

                    break;

                case "TooltipMechPerformanceMelee":
                    float melee_damage = currentChassis.MeleeDamage;
                    float melee_instability = currentChassis.MeleeInstability;

                    float dfa_damage = currentChassis.DFADamage * 2;
                    float dfa_instability = currentChassis.DFAInstability;
                    float dfa_self_damage = currentChassis.DFASelfDamage;

                    float support_damage = 0;
                    float support_heat = 0;

                    foreach (MechComponentRef mechComponentRef in currentMech.Inventory)
                    {
                        if (mechComponentRef.Def == null)
                            mechComponentRef.RefreshComponentDef();

                        // Take Melee/DFA upgrades into account
                        if (mechComponentRef.Def.statusEffects != null)
                            foreach(EffectData effect in mechComponentRef.Def.statusEffects)
                            {
                                if (effect.effectType != EffectType.StatisticEffect)
                                    continue;

                                if(effect.statisticData.targetWeaponSubType == WeaponSubType.Melee)
                                {
                                    if(effect.statisticData.statName == "DamagePerShot")
                                        BTStatistics.ApplyEffectStatistic(effect.statisticData, ref melee_damage);
                                    else if (effect.statisticData.statName == "Instability")
                                        BTStatistics.ApplyEffectStatistic(effect.statisticData, ref melee_instability);
                                }
                                else if(effect.statisticData.targetWeaponSubType == WeaponSubType.DFA)
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
                            support_heat += weapon.HeatDamage * weapon.ShotsWhenFired;
                        }

                    }

                    extra_stats += string.Format("Melee damage: <b>{0}</b> ( Stability: <b>{1}</b> )\n", (int)melee_damage, (int)melee_instability);

                    if (BTMechDef.GetJumpJetsAmount(currentMech) > 0)
                    {
                        extra_stats += string.Format("DFA damage: <b>{0}</b> ( Stability: <b>{1}</b> )\n", (int)dfa_damage, (int)dfa_instability);
                        extra_stats += string.Format("DFA self-damage: <b>{0}</b> ( per leg )\n", (int)dfa_self_damage);
                    }

                    if (support_damage > 0)
                        if (support_heat > 0)
                            extra_stats += string.Format("Support weapons damage: <b>{0}</b> ( <b>{1} H</b> )\n", support_damage, support_heat);
                        else
                            extra_stats += string.Format("Support weapons damage: <b>{0}</b>\n", support_damage);

                    break;
                
                // No idea what to put on those two. Feel free to contribute.
                case "TooltipMechPerformanceRange":
                    break;

                case "TooltipMechPerformanceDurability":
                    break;

            }

            if(extra_stats.Length != 0)
            {
                TextMeshProUGUI body = (TextMeshProUGUI)ReflectionHelper.GetPrivateField(__instance, "body");
                body.text = string.Format("{0}\n{1}", extra_stats, body.text);
            }

        }
    }

    // Keep track of currently selected mech, if any.
    [HarmonyPatch(typeof(BattleTech.UI.MechBayPanel), "ViewMechStorage")]
    public static class Patch_BattleTech_UI_MechBayPanel_ViewMechStorage
    {
        static void Postfix()
        {
            ModBase.currentMech = (MechDef)null;
        }
    }

    [HarmonyPatch(typeof(BattleTech.UI.MechBayPanel), "ViewInventory")]
    public static class Patch_BattleTech_UI_MechBayPanel_ViewInventory
    {
        static void Postfix()
        {
            ModBase.currentMech = (MechDef)null;
        }
    }

    [HarmonyPatch(typeof(BattleTech.UI.MechBayMechInfoWidget), "SetDescriptions")]
    public static class Patch_BattleTech_UI_MechBayMechInfoWidget_SetDescriptions
    {
        static void Postfix(MechBayMechInfoWidget __instance)
        {
            ModBase.Sim = __instance.sim;
            ModBase.mechBay = (MechBayPanel)ReflectionHelper.GetPrivateField(__instance, "mechBay");
            ModBase.currentMech = (MechDef)ReflectionHelper.GetPrivateField(__instance, "selectedMech");
        }
    }

    [HarmonyPatch(typeof(BattleTech.UI.MechLabStatBlockWidget), "SetData")]
    public static class Patch_BattleTech_UI_MechLabStatBlockWidget_SetData
    {
        static void Postfix(MechDef mechDef)
        {
            ModBase.inMechLab = true;
            ModBase.previousMech = ModBase.currentMech;
            ModBase.currentMech = mechDef;
        }
    }

    [HarmonyPatch(typeof(BattleTech.UI.MechLabPanel), "ExitMechLab")]
    public static class Patch_BattleTech_UI_MechLabPanel_ExitMechLab
    {
        static void Postfix()
        {
            ModBase.inMechLab = false;
            ModBase.currentMech = ModBase.previousMech;
        }
    }



    public static class ReflectionHelper
    {
        public static object InvokePrivateMethode(object instance, string methodname, object[] parameters)
        {
            Type type = instance.GetType();
            MethodInfo methodInfo = type.GetMethod(methodname, BindingFlags.NonPublic | BindingFlags.Instance);
            return methodInfo.Invoke(instance, parameters);
        }

        public static object InvokePrivateMethode(object instance, string methodname, object[] parameters, Type[] types)
        {
            Type type = instance.GetType();
            MethodInfo methodInfo = type.GetMethod(methodname, BindingFlags.NonPublic | BindingFlags.Instance, null, types, null);
            return methodInfo.Invoke(instance, parameters);
        }

        public static void SetPrivateProperty(object instance, string propertyname, object value)
        {
            Type type = instance.GetType();
            PropertyInfo property = type.GetProperty(propertyname, BindingFlags.NonPublic | BindingFlags.Instance);
            property.SetValue(instance, value, null);
        }

        public static object GetPrivateProperty(object instance, string propertyname)
        {
            Type type = instance.GetType();
            PropertyInfo property = type.GetProperty(propertyname, BindingFlags.NonPublic | BindingFlags.Instance);
            return property.GetValue(instance, new object[] { });
        }

        public static void SetPrivateProperty(Type type, string propertyname, object value)
        {
            PropertyInfo property = type.GetProperty(propertyname, BindingFlags.NonPublic | BindingFlags.Instance);
            property.SetValue(type, value, null);
        }

        public static object GetPrivateProperty(Type type, string propertyname)
        {
            PropertyInfo property = type.GetProperty(propertyname, BindingFlags.NonPublic | BindingFlags.Instance);
            return property.GetValue(type, new object[] { });
        }

        public static void SetPrivateField(object instance, string fieldname, object value)
        {
            Type type = instance.GetType();
            FieldInfo field = type.GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(instance, value);
        }

        public static object GetPrivateField(object instance, string fieldname)
        {
            Type type = instance.GetType();
            FieldInfo field = type.GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Instance);
            return field.GetValue(instance);
        }

        public static void SetPrivateField(Type type, string fieldname, object value)
        {
            FieldInfo field = type.GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(type, value);
        }

        public static object GetPrivateField(Type type, string fieldname)
        {
              FieldInfo field = type.GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Instance);
            return field.GetValue(type);
        }
    }

    public static class BTStatistics
    {
        public static void ApplyEffectStatistic(StatisticEffectData statistic, ref float cur_value)
        {
            float parsed_float = float.Parse(statistic.modValue);

            switch (statistic.operation)
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

        public static void ApplyEffectStatistic(StatisticEffectData statistic, ref int cur_value)
        {
            float parsed_float = float.Parse(statistic.modValue);
            int parsed_int = (int)parsed_float;

            switch (statistic.operation)
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
    }

    public static class ModBase
    {
        public static MechDef currentMech;
        public static MechDef previousMech;
        public static bool inMechLab = false;
        public static MechBayPanel mechBay;
        public static CombatGameConstants combatConstants;
        public static SimGameState Sim;

        public static void Init()
        {
            var harmony = HarmonyInstance.Create("org.null.ACCount.ExtendedInformation");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

    }
}
