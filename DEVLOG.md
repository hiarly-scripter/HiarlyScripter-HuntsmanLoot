# DEVLOG — Huntsman Loot (HiarlyScripter)

## Contexto

Este mod foi desenvolvido como substituto 100% original ao `DougHRito-HunterDropsGun` v1.0.7, que parou de funcionar com REPO v0.4.x. O código foi escrito do zero, sem reaproveitamento de código-fonte do mod original.

---

## O que estava quebrado no mod de referência

### Problema 1 — Detecção de morte pelo timer (`DespawnedTimer > 310f`)

**O que o mod original fazia:**
Verificava se o Huntsman havia morrido testando se `DespawnedTimer > 310f`. A lógica era indireta: quando o inimigo morria, o código original do jogo multiplicava o timer por 3, fazendo-o ultrapassar 310.

```csharp
// Código antigo (frágil)
if (!(___DespawnedTimer > 310f)) return;
```

**Por que quebrou:**
Em REPO v0.4.x, os multiplicadores e limites do `DespawnedTimer` foram ajustados. A multiplicação ainda acontece em caso de morte, mas os valores resultantes deixaram de ultrapassar consistentemente o limiar de 310. Em casos de morte legítima, o mod simplesmente não disparava o drop.

**Como foi resolvido:**
Verificação direta da saúde do inimigo via reflexão:

```csharp
// Código novo (direto e confiável)
bool hasHealth = (bool)_hasHealthField.GetValue(enemy);
EnemyHealth health = (EnemyHealth)_healthField.GetValue(enemy);
int hp = (int)_hpCurrentField.GetValue(health);
if (hp > 0) return; // despawn normal, não morte
```

Os campos `Enemy.HasHealth`, `Enemy.Health` e `EnemyHealth.healthCurrent` são `internal` no assembly do jogo, portanto acessados via `AccessTools.Field` com cache estático para eficiência.

---

### Problema 2 — Todos os arquivos desabilitados pelo r2modman (extensão `.old`)

**O que aconteceu:**
O r2modman detectou uma incompatibilidade e desabilitou todos os arquivos do mod adicionando `.old` ao final de cada nome. Isso indica que o mod causava exceções não tratadas no BepInEx ao inicializar.

**Como foi resolvido:**
Com a verificação de morte corrigida e os campos internos acessados com segurança, o mod carrega sem exceções.

---

### Problema 3 — Campo `Enemy` em `EnemyParent` é privado/interno

**O que o mod original fazia:**
Acessava o campo `Enemy` dentro de `EnemyParent` via `AccessTools.Field` chamado a cada patch — sem cache.

**Como foi resolvido:**
O campo `_enemyField` é cacheado como `static readonly` na inicialização do plugin, evitando lookups repetidos de reflexão durante o jogo.

---

## Resumo técnico das mudanças

| Componente | Antes (frágil) | Depois (confiável) |
|---|---|---|
| Detecção de morte | `DespawnedTimer > 310f` | `EnemyHealth.healthCurrent <= 0` |
| Acesso a campos internos | `AccessTools.Field` inline | `static readonly FieldInfo` cacheados |
| Compatibilidade multiplayer | `PhotonNetwork.InstantiateRoomObject` | Mantido (ainda funciona em v4.x) |

---

## v1.1.0 — Remoção de dependência externa (DougHRito-HunterGun)

**Motivação:** O mod v1.0.0 dependia do `DougHRito-HunterGun` para o modelo da espingarda. Isso tornava o mod dependente de um terceiro para funcionar — se o HunterGun fosse descontinuado ou quebrado, o HuntsmanLoot pararia de funcionar junto.

**Descoberta:** O jogo R.E.P.O. possui nativamente o item `item_gun_shotgun` registrado no `StatsManager`, com prefab próprio. Este é o mesmo modelo visual de espingarda usado pelo Huntsman e pelo shopkeeper no jogo base.

**O que mudou:**

| Componente | Antes (v1.0.0) | Depois (v1.1.0) |
|---|---|---|
| Arma dropada | `"Gun Hunter"` (DougHRito) | `item_gun_shotgun` (nativo do jogo) |
| Dependência | BepInEx + HunterGun | Apenas BepInEx |
| Busca do prefab | `Resources.FindObjectsOfTypeAll` por nome | `StatsManager.itemDictionary` via reflexão |
| Fallback | Nenhum | `ShopManager.GetAllItemsFromStatsManager` (hook existente) |
| Spawn singleplayer | `Object.Instantiate(RiflePrefab, ...)` | `Object.Instantiate(item.prefab, ...)` |
| Spawn multiplayer | `PhotonNetwork.InstantiateRoomObject("Items/Gun Hunter", ...)` | `PhotonNetwork.InstantiateRoomObject(item.prefabName, ...)` |
| Config ShopAvailable | Filtrava `"Gun Hunter"` | Removida — item nativo aparece normalmente na loja |

**Por que é 100% do HiarlyScripter:** A espingarda pertence ao jogo base, não a nenhum outro modder. O código de busca, detecção de morte e spawn é 100% original.

---

## v1.1.1 — 2026-05-19 — Correção de barra verde (ItemBattery)

**Causa:** A espingarda spawnada via `Resources.Load` / `PhotonNetwork.InstantiateRoomObject` nasce com `batteryLife = 1.0` (100% cheio) no componente `ItemEquippable`. Essa barra verde fica visível sobre o item no chão e nunca depleta, pois a espingarda não usa o sistema de bateria de forma alguma. A barra sobrepunha visualmente a barra amarela de munição, ocultando a animação de recarga com cristal.

**Correção:** Patch Harmony `Postfix` em `ItemEquippable.Start()`. Quando o item pai é `item_gun_shotgun`, o campo `suppressBatteryUI` é setado para `true` via reflexão, ocultando a barra para todos os clientes (host e guests) sem afetar a mecânica de munição.

**Campo utilizado:** `ItemEquippable.suppressBatteryUI` (detectado via strings no `Assembly-CSharp.dll`).

**Escopo:** Afeta toda instância da espingarda — tanto dropada pelo Huntsman quanto comprada na loja. Como a barra nunca depletura de qualquer forma, sumir com ela é correto em ambos os casos.

---

## Arquitetura do mod

- **`Core.cs`** — entrada BepInEx, configurações, busca do item nativo, dois patches Harmony
