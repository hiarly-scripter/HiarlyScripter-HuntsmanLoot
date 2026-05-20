# Changelog — Huntsman Loot

---

## v1.1.2 — 2026-05-20
**Compatibilidade:** R.E.P.O. Build `23250495` · BepInEx `5.4.23.5`

### Adicionado
- Config `RandomizeAmmo` — a espingarda pode cair com quantidade aleatória de balas (1 até o máximo); desative para sempre cair cheia

### Corrigido
- Espingarda agora dropa corretamente — nome do item no registro do jogo é `"Item Gun Shotgun"`, não `"item_gun_shotgun"`
- Drop no singleplayer usa `Resources.Load` em vez de `Object.Instantiate` direto (Item é ScriptableObject, não MonoBehaviour)
- Drop no multiplayer usa `resourcePath = "Items/Item Gun Shotgun"` confirmado via `PrefabRef` do jogo
- Barra de bateria verde suprimida corretamente — patch movido para `ItemBattery.Start()` com verificação via `gameObject.name` (correção: Item é ScriptableObject, `GetComponentInParent<Item>()` sempre retornava null)
- Warning `"Item 'Gun Hunter' not found in the itemDictionary"` suprimido — bug do próprio jogo ao inicializar o Huntsman; não afeta funcionalidade

---

## v1.1.1 — 2026-05-19
**Compatibilidade:** R.E.P.O. Build `23250495` · BepInEx `5.4.23.5`

### Corrigido
- Removida ofuscação do binário para conformidade com as políticas do Thunderstore

---

## v1.1.0 — 2026-05-19
**Compatibilidade:** R.E.P.O. Build `23250495` · BepInEx `5.4.23.5`

### Adicionado
- Configuração `MasterClientOnly` — controla qual jogador processa o drop em multiplayer (recomendado: `true`)

### Alterado
- A espingarda dropada agora usa o item nativo do jogo — sem dependência de mods externos

### Removido
- Dependência obrigatória do mod `DougHRito-HunterGun`

### Corrigido
- Adicionado fallback via hook `ShopManager.GetAllItemsFromStatsManager` para garantir que o item seja registrado corretamente

---

## v1.0.0
**Compatibilidade:** R.E.P.O. Build `23250495` · BepInEx `5.4.23.5`

### Adicionado
- Lançamento inicial
- Drop da espingarda ao eliminar o Huntsman
- Chance de drop configurável de 1% a 100%
- Suporte a multiplayer via Photon — drop visível para todos os jogadores na sala
- Modo berserk opcional — drop exclusivo de Huntsmans em modo berserk (requer `BerserkerEnemies`)
