# Dependências — Huntsman Loot

Este mod precisa de outros mods instalados para funcionar corretamente. **Sem eles, a espingarda não vai aparecer no jogo.**

---

## Obrigatórias

### 1. BepInExPack
**O que é:** Framework base que permite rodar mods no R.E.P.O.
**Onde baixar:** [Thunderstore — BepInExPack](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/)
**Como instalar:** Pelo r2modman, basta procurar "BepInExPack" e clicar em instalar. Se você já roda outros mods, provavelmente já tem.

---

### 2. HunterGun (por DougHRito)
**O que é:** Adiciona o modelo 3D e o item da espingarda do Huntsman ao jogo. O Huntsman Loot usa esse asset para gerar a arma no chão quando o Huntsman morre. **Sem esse mod, nenhuma espingarda vai aparecer.**
**Onde baixar:** [Thunderstore — HunterGun](https://thunderstore.io/c/repo/p/DougHRito/HunterGun/)
**Versão mínima:** 1.0.2
**Como instalar:** Pelo r2modman, procure "HunterGun" por DougHRito e instale. O r2modman cuida das dependências automaticamente se você instalar o Huntsman Loot por lá.

---

## Opcionais

### BerserkerEnemies
**O que é:** Adiciona um modo "berserk" aos inimigos.
**Necessário apenas se:** Você ativar a opção `BerserkerOnly = true` no arquivo de configuração do Huntsman Loot. Com essa opção, a espingarda só cai de um Huntsman no modo berserk.
**Sem esse mod mas com `BerserkerOnly = true`:** O Huntsman Loot vai exibir um aviso no log e dropar a espingarda normalmente de qualquer Huntsman.

---

## Verificando se está tudo instalado

1. Abra o r2modman e veja a lista de mods ativos no perfil.
2. Confirme que **BepInExPack** e **HunterGun** estão na lista e **habilitados**.
3. Inicie o jogo pelo r2modman (não diretamente pelo Steam).
4. Para confirmar que o mod carregou, abra o arquivo `BepInEx/LogOutput.log` e procure por: `Huntsman Loot v1.0.0 carregado.`

---

## Ordem de instalação recomendada

1. BepInExPack
2. HunterGun
3. Huntsman Loot
