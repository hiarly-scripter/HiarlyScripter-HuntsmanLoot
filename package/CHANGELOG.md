# Changelog — Huntsman Loot

## v1.1.2 (2026-05-19)
- Removida ofuscação do binário para conformidade com as políticas do Thunderstore

## v1.1.1 (2026-05-19)
- Corrigida barra verde (bateria) que aparecia sobre a arma no chão e nunca depleta
- A barra verde sobrepunha a barra amarela de munição, ocultando a animação de recarga com cristal

## v1.1.0 (2026-05-19)
- Removida dependência do mod DougHRito-HunterGun
- A espingarda dropada agora usa o item nativo do jogo (`item_gun_shotgun`)
- Adicionado fallback via hook `ShopManager.GetAllItemsFromStatsManager`
- Adicionada configuração `MasterClientOnly` para controle de drops em multiplayer

## v1.0.0
- Lançamento inicial
- Drop da espingarda ao matar o Huntsman
- Chance de drop configurável (1–100%)
- Suporte a multiplayer via Photon
- Modo berserk opcional (requer BerserkerEnemies)
