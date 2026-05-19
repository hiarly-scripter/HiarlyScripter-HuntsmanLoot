using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace HuntsmanLoot
{
    [BepInPlugin("com.hiarlyscripter.huntsmanloot", "Huntsman Loot", "1.1.1")]
    public sealed class HuntsmanLootPlugin : BaseUnityPlugin
    {
        internal static HuntsmanLootPlugin Instance { get; private set; }
        internal static ManualLogSource Log         { get; private set; }

        // ── Seção: Drop ───────────────────────────────────────────────────────
        internal static ConfigEntry<int>  DropChance;
        internal static ConfigEntry<bool> BerserkerOnly;
        internal static ConfigEntry<bool> MasterClientOnly;

        // Item nativo do jogo (espingarda do Huntsman — item_gun_shotgun)
        // Sem dependência de mods externos. Capturado do registro de itens do próprio jogo.
        internal static Item NativeShotgunItem;

        // Reflexão para campos internos do jogo (cache estático — resolve uma vez)
        internal static readonly FieldInfo _hasHealthField       = AccessTools.Field(typeof(Enemy),        "HasHealth");
        internal static readonly FieldInfo _healthField          = AccessTools.Field(typeof(Enemy),        "Health");
        internal static readonly FieldInfo _hpCurrentField       = AccessTools.Field(typeof(EnemyHealth),  "healthCurrent");
        internal static readonly FieldInfo _enemyField           = AccessTools.Field(typeof(EnemyParent),  "Enemy");
        internal static readonly FieldInfo _itemDictField        = AccessTools.Field(typeof(StatsManager), "itemDictionary");

        // Reflexão para campos privados de PrefabRef (usado por Item.prefab)
        internal static readonly FieldInfo _prefabNameField      = AccessTools.Field(typeof(PrefabRef), "prefabName");
        internal static readonly FieldInfo _resourcePathField    = AccessTools.Field(typeof(PrefabRef), "resourcePath");

        // Suprime a barra verde (bateria) na espingarda — ela não usa bateria de forma significativa
        internal static readonly FieldInfo _suppressBatteryField = AccessTools.Field(typeof(ItemEquippable), "suppressBatteryUI");

        private void Awake()
        {
            Instance = this;
            Log      = Logger;

            // ── Drop ──
            DropChance = Config.Bind(
                "Drop", "DropChance", 100,
                new ConfigDescription(
                    "Chance (%) de a espingarda cair quando o Huntsman morre. " +
                    "100 = sempre cai, 1 = raríssimo. Editável pelo REPOConfig.",
                    new AcceptableValueRange<int>(1, 100)));

            BerserkerOnly = Config.Bind(
                "Drop", "BerserkerOnly", false,
                "true  = a espingarda só cai de um Huntsman no modo berserk " +
                "(requer mod BerserkerEnemies — sem ele, dropa normalmente).\n" +
                "false = a espingarda cai de qualquer Huntsman (padrão).");

            MasterClientOnly = Config.Bind(
                "Drop", "MasterClientOnly", true,
                "true  = apenas o HOST/dono da sala processa o drop e gera a espingarda. " +
                "Recomendado: evita múltiplas espingardas aparecendo ao mesmo tempo em multiplayer.\n" +
                "false = qualquer cliente pode gerar o drop (pode causar duplicatas).");

            new Harmony("com.hiarlyscripter.huntsmanloot").PatchAll(typeof(HuntsmanPatches));
            Log.LogInfo("Huntsman Loot v1.1.1 carregado.");

            // Inicia busca pelo item nativo de espingarda do jogo
            StartCoroutine(WaitForNativeShotgun());
        }

        // Busca o item nativo item_gun_shotgun no registro do StatsManager.
        // Tenta via itemDictionary direto; o patch GetAllItemsFromStatsManager serve de fallback.
        private IEnumerator WaitForNativeShotgun()
        {
            while (NativeShotgunItem == null)
            {
                try
                {
                    var sm = StatsManager.instance;
                    if (sm != null && _itemDictField != null)
                    {
                        var dict = _itemDictField.GetValue(sm) as System.Collections.IDictionary;
                        if (dict != null)
                        {
                            foreach (System.Collections.DictionaryEntry kv in dict)
                            {
                                if (kv.Value is Item it && it.name == "item_gun_shotgun")
                                {
                                    NativeShotgunItem = it;
                                    var pn = (string)_prefabNameField?.GetValue(it.prefab);
                                    var rp = (string)_resourcePathField?.GetValue(it.prefab);
                                    Log.LogInfo(
                                        $"[HuntsmanLoot] Espingarda nativa encontrada: " +
                                        $"name={it.name} prefabName={pn} resourcePath={rp}");
                                    yield break;
                                }
                            }
                        }
                    }
                }
                catch { /* StatsManager ainda não inicializado */ }

                yield return new WaitForSeconds(1f);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  HuntsmanPatches — patches Harmony
    // ──────────────────────────────────────────────────────────────────────────
    [HarmonyPatch]
    internal static class HuntsmanPatches
    {
        // ── 1. Detecta morte do Huntsman e dropa a espingarda ─────────────────
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyParent), "Despawn")]
        static void OnHuntsmanDespawn(EnemyParent __instance)
        {
            if (__instance.enemyName != "Huntsman") return;

            // Garante que só o host processa o drop em multiplayer (evita duplicatas)
            if (HuntsmanLootPlugin.MasterClientOnly.Value && !SemiFunc.IsMasterClientOrSingleplayer()) return;

            Enemy enemy = (Enemy)HuntsmanLootPlugin._enemyField.GetValue(__instance);
            if (enemy == null) return;

            bool hasHealth = (bool)HuntsmanLootPlugin._hasHealthField.GetValue(enemy);
            if (!hasHealth) return;

            EnemyHealth health = (EnemyHealth)HuntsmanLootPlugin._healthField.GetValue(enemy);
            if (health == null) return;

            int hp = (int)HuntsmanLootPlugin._hpCurrentField.GetValue(health);
            if (hp > 0) return; // despawn normal, não morte

            if (UnityEngine.Random.Range(1, 101) > HuntsmanLootPlugin.DropChance.Value) return;

            if (HuntsmanLootPlugin.BerserkerOnly.Value)
                AttemptBerserkerDrop(enemy);
            else
                DropRifle(enemy);
        }

        private static void AttemptBerserkerDrop(Enemy enemy)
        {
            Assembly berserkAsm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "BerserkerEnemies");

            if (berserkAsm == null)
            {
                HuntsmanLootPlugin.Log.LogWarning(
                    "[HuntsmanLoot] BerserkerEnemies não detectado — dropando mesmo assim.");
                DropRifle(enemy);
                return;
            }

            Type ctrlType = berserkAsm.GetType("BerserkerController");
            if (ctrlType == null) return;

            Component ctrl = ((Component)enemy).GetComponentInParent(ctrlType);
            if (ctrl == null) return;

            FieldInfo flag = ctrlType.GetField("isBerserkerFlag");
            if (flag == null) return;

            if (flag.GetValue(ctrl) is bool isBerserker && isBerserker)
                DropRifle(enemy);
        }

        internal static void DropRifle(Enemy enemy)
        {
            Transform origin = enemy.CustomValuableSpawnTransform ?? enemy.CenterTransform;

            var shotgun = HuntsmanLootPlugin.NativeShotgunItem;
            if (shotgun == null)
            {
                HuntsmanLootPlugin.Log.LogWarning(
                    "[HuntsmanLoot] item_gun_shotgun não encontrado no registro do jogo — drop ignorado.");
                return;
            }

            var resourcePath = (string)HuntsmanLootPlugin._resourcePathField?.GetValue(shotgun.prefab);
            var prefabName   = (string)HuntsmanLootPlugin._prefabNameField?.GetValue(shotgun.prefab);

            if (!SemiFunc.IsMultiplayer())
            {
                // Singleplayer: carrega via resourcePath e instancia localmente
                if (string.IsNullOrEmpty(resourcePath))
                {
                    HuntsmanLootPlugin.Log.LogWarning("[HuntsmanLoot] resourcePath vazio — drop ignorado.");
                    return;
                }
                var go = Resources.Load<GameObject>(resourcePath);
                if (go != null)
                    UnityEngine.Object.Instantiate(go, origin.position, Quaternion.identity);
                else
                    HuntsmanLootPlugin.Log.LogWarning(
                        $"[HuntsmanLoot] Resources.Load falhou para '{resourcePath}' — drop ignorado.");
            }
            else
            {
                // Multiplayer: Photon usa prefabName como chave de instância
                if (string.IsNullOrEmpty(prefabName))
                {
                    HuntsmanLootPlugin.Log.LogWarning("[HuntsmanLoot] prefabName vazio — drop ignorado.");
                    return;
                }
                PhotonNetwork.InstantiateRoomObject(
                    prefabName, origin.position, Quaternion.identity, 0, null);
            }
        }

        // ── 2. Remove barra verde (bateria) da espingarda ────────────────────
        // A espingarda não usa bateria de forma alguma; a barra verde fica em 1.0
        // para sempre e sobrepõe visualmente a barra amarela de munição.
        // Roda em todos os clientes via Start(), corrigindo o bug em multiplayer também.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemEquippable), "Start")]
        static void SuppressShotgunBatteryUI(ItemEquippable __instance)
        {
            if (HuntsmanLootPlugin._suppressBatteryField == null) return;
            var item = __instance.GetComponentInParent<Item>();
            if (item != null && item.name == "item_gun_shotgun")
                HuntsmanLootPlugin._suppressBatteryField.SetValue(__instance, true);
        }

        // ── 3. Fallback: captura item nativo via hook da loja ─────────────────
        // Garante que o item é capturado mesmo se StatsManager não estava pronto
        // no Awake. Também cobre o caso de morte do Huntsman antes da primeira loja.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShopManager), "GetAllItemsFromStatsManager")]
        static void CaptureNativeShotgun(ref List<Item> ___potentialItems)
        {
            if (HuntsmanLootPlugin.NativeShotgunItem == null)
            {
                var found = ___potentialItems.FirstOrDefault(it => it.name == "item_gun_shotgun");
                if (found != null)
                {
                    HuntsmanLootPlugin.NativeShotgunItem = found;
                    var pn = (string)HuntsmanLootPlugin._prefabNameField?.GetValue(found.prefab);
                    HuntsmanLootPlugin.Log.LogInfo(
                        $"[HuntsmanLoot] Espingarda capturada via loja: name={found.name} prefabName={pn}");
                }
            }
        }
    }
}
