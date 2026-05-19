# 🔫 Huntsman Loot

[![Thunderstore](https://img.shields.io/badge/Thunderstore-v1.1.2-brightgreen?style=flat-square&logo=thunderstore)](https://thunderstore.io/c/repo/p/HiarlyScripter/HuntsmanLoot/)
[![R.E.P.O.](https://img.shields.io/badge/R.E.P.O.-Build%2023250495-blue?style=flat-square)](https://store.steampowered.com/app/3241660/REPO/)
[![BepInEx](https://img.shields.io/badge/BepInEx-5.4.23.5-yellow?style=flat-square)](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/)
[![Licença](https://img.shields.io/badge/licença-crédito%20obrigatório-red?style=flat-square)](LICENSE)

> O Huntsman agora **paga um preço** quando você o elimina — ele larga sua espingarda no chão!

---

## ✨ O que faz

Faz o Huntsman dropar sua espingarda (`item_gun_shotgun`) ao ser eliminado. Usa o item nativo do próprio jogo — **sem dependência de nenhum outro mod**.

---

## 🎯 Funcionalidades

- 🔫 **Drop ao morrer** — a espingarda cai exatamente onde o Huntsman morreu
- 🎲 **Chance configurável** — de 1% a 100% (padrão: sempre dropa)
- 💀 **Modo Berserk** — drop exclusivo de Huntsmans em modo berserk *(requer BerserkerEnemies, opcional)*
- 👑 **Processado pelo host** — só o host processa o drop; a arma aparece para **todos na sala**
- 🔧 **Zero dependências extras** — usa o item nativo do jogo, nada mais
- ⚙️ **REPOConfig** — todos os configs editáveis dentro do jogo *(opcional)*

---

## 👥 Multiplayer

> **Somente o host** precisa ter o mod instalado. A espingarda aparece para **todos os jogadores**.

| Cenário | Resultado |
|---|---|
| ✅ Só o host tem o mod | Drop funciona — host spawna para todos via rede |
| ✅ Todos têm o mod | Drop funciona normalmente |
| ❌ Ninguém tem o mod | Sem drop |

---

## ⚙️ Configurações

| Seção | Chave | Padrão | Descrição |
|---|---|---|---|
| `Drop` | `DropChance` | `100` | Chance (%) de drop — 1 a 100 |
| `Drop` | `BerserkerOnly` | `false` | Só dropa de Huntsmans em modo berserk |
| `Drop` | `MasterClientOnly` | `true` | Recomendado — evita drops duplicados em multiplayer |

---

## 📦 Instalação

**Via r2modman (recomendado):**
1. Instale o **BepInExPack**
2. Procure e instale o **Huntsman Loot** no Thunderstore
3. *(Opcional)* Instale o **REPOConfig**

**Via manual:**
1. Instale o BepInExPack
2. Copie `plugins/HiarlyScripter-HuntsmanLoot/` para `BepInEx/plugins/`

---

## 📄 Licença

[Licença customizada](LICENSE) — uso e estudo permitidos. **Crédito ao autor obrigatório** em qualquer redistribuição ou trabalho derivado.

---

*Mod criado por **[HiarlyScripter](https://discord.com/users/hiarly_ferreira)** · Testado com R.E.P.O. Build `23250495` · BepInEx `5.4.23.5`*
