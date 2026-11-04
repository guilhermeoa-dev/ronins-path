# Ronin`s Path

Jogo de plataforma e combate 2D desenvolvido em **Unity 2017.4.40f1**, com scripts em C#, para o projeto escolar de Jogos do 4º bimestre.

O samurai atravessa fases, enfrenta guardas e um mago chefe, usa katana e três tipos de projéteis e interage com caixas, plataformas e objetos destrutíveis.

## Como abrir e jogar

1. Clone ou baixe este repositório.
2. Abra a pasta do projeto no **Unity 2017.4.40f1**.
3. Aguarde a importação dos assets. O Unity recria a pasta `Library` automaticamente.
4. Abra a cena `Assets/PrimeiraTela.unity` e pressione **Play**.
5. Use o menu e a seleção de fases na tela.

O repositório contém o projeto editável. Não inclui executável.

## Controles

| Ação | Tecla |
| --- | --- |
| Andar | A / D ou setas |
| Correr | Shift |
| Pular | Espaço |
| Dash | Q |
| Katana | Z |
| Escolher projétil | 1 / 2 / 3 |
| Disparar | X |
| Defender de projéteis | Segurar F |
| Carregar / soltar caixa | Segurar / soltar E |
| Reiniciar a missão | R |
| Voltar à seleção durante a missão | Esc |

A disponibilidade das ações depende do estado do personagem. A defesa de projéteis não bloqueia o golpe próximo do chefe.

## Recursos do projeto

- Combate corpo a corpo com janela de dano e controle de acertos repetidos.
- Três tipos de projéteis, incluindo explosão em área.
- Guardas com patrulha, perseguição, alcance de detecção e linha de visão.
- Mago chefe com ataque distante e ataque próximo.
- Defesa ligada à animação do personagem.
- Objetos destrutíveis e recompensas.
- HUD de vida, menus, seleção de fases e telas de resultado.
- Efeitos sonoros e mudança de música no confronto com o chefe.
- Gizmos para visualizar alcance e perseguição no editor.

## Organização

- `Assets/`: cenas, scripts, sprites, animações e áudio. Os arquivos `.meta` preservam as referências do Unity.
- `Assets/Scripts/`: lógica de execução do jogo.
- `Assets/Editor/`: ferramentas e verificações do projeto, quando presentes.
- `ProjectSettings/`: configurações do Unity.
- `UnityPackageManager/`: manifesto do gerenciador usado nesta versão do projeto.
- `docs/`: guia de estudo dos códigos.

## Documentação

[Guia em PDF: requisitos e código explicado linha por linha](docs/Ronins_Path_Guia_Codigos_4_Bimestre.pdf)

O guia apresenta as cinco exigências de combate do 4º bimestre e 35 funções/propriedades selecionadas. A compilação da versão formatada foi verificada no Unity 2017.4.40f1.

## Créditos e uso

Projeto acadêmico de Guilherme Oliveira. Inclui recursos de terceiros; os direitos sobre imagens, sprites e músicas pertencem aos respectivos titulares. Este repositório não concede uma licença de redistribuição desses recursos.
