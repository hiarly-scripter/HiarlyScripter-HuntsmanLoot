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
    [BepInPlugin("com.hiarlyscripter.huntsmanloot", "Huntsman Loot", "1.1.3")]
    public sealed class HuntsmanLootPlugin : BaseUnityPlugin
    {
        internal static HuntsmanLootPlugin Instance { get; private set; }
        internal static ManualLogSource Log         { get; private set; }

        // ── Secao: Drop ───────────────────────────────────────────────────────
        internal static ConfigEntry<int>  DropChance;
        internal static ConfigEntry<bool> BerserkerOnly;
        internal static ConfigEntry<bool> MasterClientOnly;
        internal static ConfigEntry<bool> RandomizeAmmo;

        // Item nativo do jogo (espingarda do Huntsman - ScriptableObject capturado do registro)
        internal static Item NativeShotgunItem;

        // Reflexao - campos internos do jogo (cache estatico, resolve uma vez na inicializacao)
        internal static readonly FieldInfo _hasHealthField       = AccessTools.Field(typeof(Enemy),        "HasHealth");
        internal static readonly FieldInfo _healthField          = AccessTools.Field(typeof(Enemy),        "Health");
        internal static readonly FieldInfo _hpCurrentField       = AccessTools.Field(typeof(EnemyHealth),  "healthCurrent");
        internal static readonly FieldInfo _enemyField           = AccessTools.Field(typeof(EnemyParent),  "Enemy");
        internal static readonly FieldInfo _itemDictField        = AccessTools.Field(typeof(StatsManager), "itemDictionary");

        // Reflexao - PrefabRef (Item.prefab)
        internal static readonly FieldInfo _prefabNameField      = AccessTools.Field(typeof(PrefabRef), "prefabName");
        internal static readonly FieldInfo _resourcePathField    = AccessTools.Field(typeof(PrefabRef), "resourcePath");

        // Reflexao - Item ScriptableObject
        internal static readonly FieldInfo _itemNameField        = AccessTools.Field(typeof(Item), "itemName");

        // Reflexao - ItemGun (municao)
        internal static readonly FieldInfo _numberOfBulletsField = AccessTools.Field(typeof(ItemGun), "numberOfBullets");

        // Reflexao - supressao da barra de bateria
        internal static readonly FieldInfo _suppressBatteryField = AccessTools.Field(typeof(ItemBattery), "suppressBatteryUI");

        private void Awake()
        {
            Instance = this;
            Log      = Logger;

            DropChance = Config.Bind(
                "Drop", "DropChance", 100,
                new ConfigDescription(
                    "Chance (%) de a espingarda cair quando o Huntsman morre. " +
                    "100 = sempre cai, 1 = rarissimo.",
                    new AcceptableValueRange<int>(1, 100)));

            BerserkerOnly = Config.Bind(
                "Drop", "BerserkerOnly", false,
                "true  = a espingarda so cai de um Huntsman no modo berserk " +
                "(requer mod BerserkerEnemies - sem ele, dropa normalmente).\n" +
                "false = a espingarda cai de qualquer Huntsman (padrao).");

            MasterClientOnly = Config.Bind(
                "Drop", "MasterClientOnly", true,
                "true  = apenas o HOST/dono da sala processa o drop (evita duplicatas em multiplayer).\n" +
                "false = qualquer cliente pode gerar o drop.");

            RandomizeAmmo = Config.Bind(
                "Drop", "RandomizeAmmo", true,
                "true  = a espingarda cai com quantidade aleatoria de balas (entre 1 e o maximo).\n" +
                "false = a espingarda cai sempre com municao completa.");

            new Harmony("com.hiarlyscripter.huntsmanloot").PatchAll(typeof(HuntsmanPatches));
            Log.LogInfo("Huntsman Loot v1.1.3 carregado.");

            StartCoroutine(WaitForNativeShotgun());
        }

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
                        if (dict != null && dict.Count > 0)
                        {
                            foreach (System.Collections.DictionaryEntry kv in dict)
                            {
                                var key = kv.Key?.ToString() ?? "";
                                if (kv.Value is Item it &&
                                    (key == "Item Gun Shotgun" || it.name == "Item Gun Shotgun"))
                                {
                                    NativeShotgunItem = it;
                                    RegisterItemNameAlias(it, dict);
                                    Log.LogInfo("[HuntsmanLoot] Espingarda nativa encontrada no registro do jogo.");
                                    yield break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex) { Log.LogWarning($"[HuntsmanLoot] WaitForNativeShotgun: {ex.Message}"); }

                yield return new WaitForSeconds(2f);
            }
        }

        // Registra item.itemName (ex: "Gun Hunter") como alias no dicionario.
        // O itemName e o identificador interno usado pela inicializacao do item no jogo.
        internal static void RegisterItemNameAlias(Item item, System.Collections.IDictionary dict)
        {
            var itemName = (string)_itemNameField?.GetValue(item);
            if (!string.IsNullOrEmpty(itemName) && !dict.Contains(itemName))
                dict[itemName] = item;
        }

        // Aguarda um frame (Start() do ItemGun ja rodou, numberOfBullets esta no valor maximo)
        // e aplica um valor aleatorio entre 1 e esse maximo.
        internal static IEnumerator ApplyRandomAmmo(GameObject spawned)
        {
            yield return null;
            if (_numberOfBulletsField == null) yield break;
            var gun = spawned.GetComponentInChildren<ItemGun>();
            if (gun == null) yield break;
            int maxAmmo = (int)_numberOfBulletsField.GetValue(gun);
            if (maxAmmo > 1)
                _numberOfBulletsField.SetValue(gun, UnityEngine.Random.Range(1, maxAmmo + 1));
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

            if (HuntsmanLootPlugin.MasterClientOnly.Value && !SemiFunc.IsMasterClientOrSingleplayer()) return;

            Enemy enemy = (Enemy)HuntsmanLootPlugin._enemyField.GetValue(__instance);
            if (enemy == null) return;

            bool hasHealth = (bool)HuntsmanLootPlugin._hasHealthField.GetValue(enemy);
            if (!hasHealth) return;

            EnemyHealth health = (EnemyHealth)HuntsmanLootPlugin._healthField.GetValue(enemy);
            if (health == null) return;

            int hp = (int)HuntsmanLootPlugin._hpCurrentField.GetValue(health);
            if (hp > 0) return;

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
                    "[HuntsmanLoot] BerserkerEnemies nao detectado — dropando mesmo assim.");
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
                    "[HuntsmanLoot] Espingarda nao encontrada no registro do jogo — drop ignorado.");
                return;
            }

            var resourcePath = (string)HuntsmanLootPlugin._resourcePathField?.GetValue(shotgun.prefab);
            if (string.IsNullOrEmpty(resourcePath))
            {
                HuntsmanLootPlugin.Log.LogWarning("[HuntsmanLoot] resourcePath vazio — drop ignorado.");
                return;
            }

            GameObject spawned;
            if (!SemiFunc.IsMultiplayer())
            {
                var prefab = Resources.Load<GameObject>(resourcePath);
                if (prefab == null)
                {
                    HuntsmanLootPlugin.Log.LogWarning(
                        $"[HuntsmanLoot] Resources.Load falhou para '{resourcePath}' — drop ignorado.");
                    return;
                }
                spawned = UnityEngine.Object.Instantiate(prefab, origin.position, Quaternion.identity);
            }
            else
            {
                spawned = PhotonNetwork.InstantiateRoomObject(
                    resourcePath, origin.position, Quaternion.identity, 0, null);
            }

            if (HuntsmanLootPlugin.RandomizeAmmo.Value)
                HuntsmanLootPlugin.Instance.StartCoroutine(HuntsmanLootPlugin.ApplyRandomAmmo(spawned));
        }

        // ── 2. Suprime barra de bateria na espingarda ─────────────────────────
        // ItemBattery.suppressBatteryUI = true faz o UI nao renderizar a barra verde,
        // deixando a barra amarela de municao visivel sem sobreposicao.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemBattery), "Start")]
        static void SuppressShotgunBatteryUI(ItemBattery __instance)
        {
            if (HuntsmanLootPlugin._suppressBatteryField == null) return;
            // Item e ScriptableObject, nao Component — usa o nome do GameObject diretamente
            if (__instance.gameObject.name.StartsWith("Item Gun Shotgun"))
                HuntsmanLootPlugin._suppressBatteryField.SetValue(__instance, true);
        }

        // ── 3. Fallback: captura item nativo via hook da loja ─────────────────
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShopManager), "GetAllItemsFromStatsManager")]
        static void CaptureNativeShotgun(ref List<Item> ___potentialItems)
        {
            if (HuntsmanLootPlugin.NativeShotgunItem != null) return;

            var found = ___potentialItems.FirstOrDefault(it => it.name == "Item Gun Shotgun");
            if (found == null) return;

            HuntsmanLootPlugin.NativeShotgunItem = found;

            var sm = StatsManager.instance;
            if (sm != null && HuntsmanLootPlugin._itemDictField != null)
            {
                var dict = HuntsmanLootPlugin._itemDictField.GetValue(sm) as System.Collections.IDictionary;
                if (dict != null)
                    HuntsmanLootPlugin.RegisterItemNameAlias(found, dict);
            }

            HuntsmanLootPlugin.Log.LogInfo("[HuntsmanLoot] Espingarda capturada via hook da loja.");
        }

        // ── 4. Silencia warning "Gun Hunter not found in itemDictionary" ──────
        // O jogo tenta buscar a arma do Huntsman ("Gun Hunter") no dicionario de itens
        // de loja ao inicializar o inimigo. Como "Gun Hunter" nao e um item compravel,
        // a busca falha e o jogo loga esse warning a cada Huntsman spawnado.
        // E um bug do proprio jogo — suprimimos para nao poluir o log.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UnityEngine.Debug), "LogWarning", new[] { typeof(object) })]
        static bool SuppressGunHunterWarning(object message)
        {
            if (message is string s &&
                s.Contains("Gun Hunter") &&
                s.Contains("not found in the itemDictionary"))
                return false;
            return true;
        }
    }
}
