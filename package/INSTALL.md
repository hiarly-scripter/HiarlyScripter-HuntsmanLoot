# Guia de Instalação — Huntsman Loot

> ✅ Testado com **R.E.P.O. Build `23250495`** (2026-05-16) · **BepInExPack `5.4.23.5`** · **REPOConfig `1.2.6`** *(opcional)*
>
> ⚠️ Se o jogo atualizar e o mod parar de funcionar, verifique se há uma versão nova do Huntsman Loot compatível.

---

## Dependências

### Obrigatórias (instale antes do mod)

| Mod | Versão testada | Link | Para que serve |
|---|---|---|---|
| **BepInExPack** | `5.4.23.5` | [Thunderstore](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/) | Framework base — obrigatório para qualquer mod |

> ✅ **Nenhuma dependência de outros mods de conteúdo.** A espingarda que dropa é o item nativo do próprio jogo R.E.P.O. (`item_gun_shotgun`). Nenhum mod externo de arma é necessário.

### Opcionais

| Mod | Versão testada | Link | Para que serve |
|---|---|---|---|
| **REPOConfig** | `1.2.6` | [Thunderstore](https://thunderstore.io/c/repo/p/nickklmao/REPOConfig/) | Editar as configurações do mod dentro do próprio jogo, sem precisar abrir arquivos |
| **BerserkerEnemies** (Zehs) | `—` | [Thunderstore](https://thunderstore.io/c/repo/p/Zehs/BerserkerEnemies/) | Necessário apenas se você quiser usar a opção `BerserkerOnly = true` (drop exclusivo de Huntsmans no modo berserk) |

---

## Como instalar

### Opção 1 — r2modman (recomendado)

1. Abra o **r2modman** e selecione o jogo **R.E.P.O.**
2. Clique em **Online** e procure por `BepInExPack` → instale
3. Procure por `Huntsman Loot` → instale
4. *(Opcional)* Procure por `REPOConfig` → instale
5. Clique em **Start modded** para jogar

### Opção 2 — Manual

1. Instale o **BepInExPack** primeiro
2. Copie a pasta `plugins/HiarlyScripter-HuntsmanLoot/` para dentro de `BepInEx/plugins/` no diretório do jogo
3. Inicie o jogo uma vez — o arquivo de config será gerado automaticamente em `BepInEx/config/com.hiarlyscripter.huntsmanloot.cfg`

---

## Quem precisa instalar?

> **Apenas o host (dono da sala)** precisa ter o mod instalado para que o drop funcione.

Quando o Huntsman morre, o host processa o drop e spawna a espingarda pela rede do jogo (Photon). A arma aparece para **todos os jogadores na sala**, mesmo os que não têm o mod instalado.

| Cenário | Drop funciona? |
|---|---|
| Só o host tem o mod | ✅ Sim — host spawna para todos |
| Todos têm o mod | ✅ Sim |
| Ninguém tem o mod | ❌ Não |

---

## Configurações disponíveis

Edite em `BepInEx/config/com.hiarlyscripter.huntsmanloot.cfg` ou use o **REPOConfig** dentro do jogo:

| Seção | Chave | Padrão | O que faz |
|---|---|---|---|
| `Drop` | `DropChance` | `100` | Chance (%) de a espingarda cair quando o Huntsman morre. 100 = sempre, 1 = raríssimo. |
| `Drop` | `BerserkerOnly` | `false` | Se `true`, a espingarda só cai de Huntsmans no modo berserk (requer mod BerserkerEnemies). |
| `Drop` | `MasterClientOnly` | `true` | **Recomendado manter ativo.** Garante que só o host processa o drop, evitando múltiplas espingardas ao mesmo tempo em multiplayer. |

---

## Problemas comuns

| Problema | Solução |
|---|---|
| A arma não cai quando o Huntsman morre | Certifique-se de que o **host** tem o mod instalado |
| A arma aparece várias vezes | Mantenha `MasterClientOnly = true` (padrão) |
| Quero drop só de Huntsman berserk | Instale o **BerserkerEnemies** e ative `BerserkerOnly = true` |
| Log mostra "item_gun_shotgun não encontrado" | Isso não deve acontecer — reporte como bug com o log completo |

---

*Mod criado por **HiarlyScripter**.*
