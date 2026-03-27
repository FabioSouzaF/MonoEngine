# 🎮 MonoEngine

A **MonoEngine** é uma Game Engine híbrida (2D e 3D) baseada na arquitetura **Entity-Component System (ECS)**, desenvolvida inteiramente em **C#** sobre o framework **MonoGame**. Inspirada em engines modernas como a Unity, ela oferece um fluxo de trabalho com um **Editor Visual** próprio (baseado em ImGui) e suporte a **compilação dinâmica de scripts C#**.

---

## 🏗️ Arquitetura do Projeto

A engine foi projetada de forma modular, separando a lógica de edição da lógica de execução. O ecossistema é dividido em três grandes pilares:

### 1. `Engine.Core` (O Coração da Engine)
Biblioteca central onde residem os sistemas lógicos vitais do motor. Pode ser referenciada tanto pelo Editor quanto pelo Runtime final do jogo.
- **Entity-Component System (ECS):** Define a base de `GameObject`, `Component` e `Transform`. Toda entidade do jogo é um `GameObject` que ganha comportamentos dinâmicos através de seus componentes.
- **SceneManager & Serialization:** Controle de cenas virtuais e capacidade de salvar/carregar cenas via serialização (`Newtonsoft.Json`).
- **RenderManager:** Gerencia a fila de desenho utilizando `SpriteSortMode.Deferred` no `SpriteBatch`, suportando Z-Index (Camadas), matriz de câmera (`GetViewMatrix`) e otimizado com *PointClamp* para jogos em Pixel Art.
- **AssetManager:** Abstração para o carregamento de recursos. No Editor, lê arquivos nativos em disco; no Runtime, faz a leitura direto da memória através de pacotes compactados (`.pak`).

### 2. `Engine.Editor` (O Motor Visual)
Aplicativo focado no desenvolvimento do jogo, com interface gráfica gerada dinamicamente pelo **ImGui.NET**.
- **Project Hub:** Janela inicial para criar novos projetos, gerando automaticamente a estrutura de pastas (`Assets`, `Scripts`, `Scenes`) e o arquivo de configuração `.monoengine`.
- **Inspetor Dinâmico (`InspectorWindow`):** Utiliza **Reflection** para ler as propriedades e variáveis públicas dos `GameObjects` e seus `Components`, desenhando a interface adequada na tela dinamicamente. Suporta *Drag-and-Drop* de arquivos.
- **Compilador Dinâmico (`ScriptCompiler`):** Compila os scripts C# do usuário em tempo de execução acionando o *Dotnet CLI* internamente, transformando-os numa DLL temporária. Essa DLL é carregada em memória (via array de bytes) para não bloquear arquivos no HD, permitindo o famoso *Hot-Reload*.

### 3. `Engine.Runtime` (O Jogo Final)
É a aplicação leve e autossuficiente que é gerada na exportação do jogo.
- Não carrega o ImGui nem ferramentas de edição, focando apenas na performance de carregar e rodar as cenas de forma sequencial, consumindo o pacote de recursos selado gerado no momento do build.

---

## 🚀 Funcionalidades e Recursos Principais

### 🧩 Sistema de Componentes (GameObject & Component)
Todo objeto na cena nasce com um `Transform` inerente. Você pode acoplar novos comportamentos arrastando ou adicionando componentes. 
Exemplo de componentes nativos embutidos na engine:
- **SpriteRenderer:** Renderiza texturas baseadas na rotação/escala do `Transform` sem "bug de matriz zero", calcula BoundingBox visualmente e possui coloração flexível.
- **Camera:** Cria a *View Matrix* da cena, suportando centralização (Origin) e Zoom (com compatibilidade e mapeamento *ScreenToWorld*).

### 💻 Injeção de Scripts (Hot-Reloading)
Os usuários da MonoEngine podem escrever lógica própria na pasta `Scripts/`. A Engine automaticamente:
1. Cria um arquivo temporário `.csproj`.
2. Aciona o SDK do `dotnet` silenciosamente via processo `ProcessStartInfo`.
3. Carrega a lógica compilada diretamente via bytes em memória (`Assembly.Load`).
4. O novo Script aparece imediatamente para o usuário no Editor através da varredura de *Reflection*.

### 📦 Sistema de Build Integrado
Com a engine, você não precisa compilar via terminal. O `MainMenuBar` oferece exportação nativa *One-Click* (Build). A pipeline de build inclui:
1. **Cross-Platform:** Geração standalone usando os runtimes nativos do .NET (`win-x64`, `linux-x64`, `osx-x64`).
2. **Obfuscação e Proteção:** 
   - Texturas, Áudios e Arquivos da pasta `Assets` são comprimidos e blindados em um arquivo inquebrável `Assets.pak`.
   - Arquivos `.scene` contendo os dados do jogo são encriptados e convertidos num binário cego `.dat`, prevenindo engenharia reversa nos níveis do jogo.
3. **Compilação de UserScripts:** Código da pasta `Scripts` é empacotado em sua própria `UserScripts.dll` de Release para rodar de forma nativa no Runtime exportado.

### 🛡️ Segurança e Proteção de Assets (AES-256)
No momento do build, a engine não apenas empacota seu jogo, mas o blinda para distribuição em produção:
* **Criptografia:** Os arquivos `.scene` (JSONs abertos) são criptografados com o algoritmo **AES-256** e convertidos em binários `.dat` ilegíveis. Isso impede que curiosos alterem os atributos do jogo (vida do boss, velocidade do player) abrindo os arquivos em blocos de notas.
* **Empacotamento de Recursos:** Suas texturas e áudios da pasta `Assets` não ficam expostos. Eles são comprimidos e trancados em um arquivo unificado `Assets.pak`. No Runtime, a engine extrai e renderiza esses recursos diretamente da memória RAM com altíssima performance.

### ⏳ Editor Isolado com Sistema de Snapshot (Play/Stop)
O ambiente de desenvolvimento conta com um sistema inteligente e seguro de simulação. Ao dar "Play", a Engine tira um *snapshot* invisível, serializando o estado original da cena na memória RAM. Você pode testar físicas, colisões, destruir objetos e rodar lógicas livremente. Ao clicar em "Stop", a cena pisca e é instantaneamente reconstruída para o milissegundo exato antes do teste, garantindo um Level Design à prova de acidentes.

### 🖥️ Workspace Inteligente e Persistente
O layout do editor respeita o seu fluxo de trabalho. A MonoEngine salva automaticamente a disposição das suas janelas (`layout.ini`) individualmente **para cada projeto**. Você pode montar o seu *workspace* perfeito — Inspetor de um lado, Hierarquia do outro, Content Browser gigante — fechar a engine, e ela vai lembrar exatamente de como você deixou a casa quando voltar.

### 🐧 First-Class Linux Support
Desenvolvida e validada ativamente em ambiente Linux, a engine não faz gambiarras com caminhos de arquivos. Todo o fluxo de *Hot-Reloading*, injeção de dependências em memória via *Dotnet CLI* e execução do Runtime Standalone roda nativamente em Linux, tornando a MonoEngine uma ferramenta de primeira linha para a comunidade open-source.

---

## ⚙️ Requisitos do Sistema e Dependências Tecnológicas

- **.NET 9.0 SDK:** O coração do sistema operacional subjacente para a compilação do compilador local e do Runtime da aplicação.
- **MonoGame Framework DesktopGL (v3.8.1+):** Base gráfica e ciclo de atualização da engine.
- **ImGui.NET:** Biblioteca de renderização da UI retida de alto desempenho utilizada no ambiente do Editor.
- **Newtonsoft.Json:** Processamento robusto da serialização em disco da Hierarquia (Entity-Component System).

---

## 📂 Estrutura Padrão de um Projeto (MonoEngine)

Quando um projeto é instanciado, a hierarquia resultante tem esse formato:

```text
MeuNovoJogo/
│
├── project.monoengine   # Arquivo de configuração raiz do jogo
├── Assets/              # Onde vivem sprites, fontes e sons crus
├── Scripts/             # Código fonte .cs criado pelo desenvolvedor
└── Scenes/              # Cenas gráficas serilizadas pelo editor (.scene)
```

---

## 🛠️ Como Extender a Engine

### 📜 Criando Novos Componentes

Como a *MonoEngine* é extremamente reflexiva, adicionar comportamentos é tão simples quanto derivar de `Component`:

```csharp
using Engine.Core.Components;
using Microsoft.Xna.Framework;

public class MeuScriptCustomizado : Component
{
    public float Velocidade = 100f; // Ajustado para 100f para ficar mais visível no teste!

    public override void Update(GameTime gameTime)
    {
        var pos = Transform.LocalPosition;
        pos.X += Velocidade * (float)gameTime.ElapsedGameTime.TotalSeconds;
        Transform.LocalPosition = pos;
    }
}
```
*Logo após escrever esse script e salvar, ele pode ser compilado no Hub da interface gráfica da Engine e será mapeado no Menu de "Adicionar Componente".*

---

### 🛠️ Criando Ferramentas de Editor

A *MonoEngine* permite estender a interface do editor facilmente. Para criar uma nova janela ou ferramenta, siga estes passos:

1. **Crie uma nova classe** que herda de `EditorWindow` (no namespace `Engine.Editor.UI`).
2. **Implemente o método `Draw()`**, utilizando os comandos da `ImGuiNET` para desenhar sua interface.
3. **Registre a janela** no `EditorApp.cs` dentro do método `LoadContent()`.

Exemplo de uma ferramenta customizada:

```csharp
using Engine.Editor.UI;
using ImGuiNET;

public class MinhaFerramenta : EditorWindow
{
    public MinhaFerramenta()
    {
        Name = "Minha Ferramenta";
    }

    public override void Draw()
    {
        if (ImGui.Begin(Name, ref _isOpen))
        {
            ImGui.Text("Olá da minha nova ferramenta!");
            if (ImGui.Button("Clique Aqui"))
            {
                // Lógica da ferramenta
            }
            ImGui.End();
        }
    }
}
```

Para registrar no `EditorApp.cs`:
```csharp
_uiManager.AddWindow(new MinhaFerramenta());
```

---

## 🔍 Verificação Técnica Efetuada

Realizamos uma varredura completa na estrutura da MonoEngine e confirmamos a integridade dos seguintes pilares:

- ✅ **Core Modules:** `RenderManager`, `SceneManager` e `AssetManager` estão devidamente isolados e funcionais.
- ✅ **ECS System:** As classes base `GameObject`, `Component` e `Transform` seguem o padrão de composição esperado.
- ✅ **Editor Architecture:** O sistema de janelas baseado em `EditorWindow` e `EditorUIManager` permite extensibilidade sem acoplamento rígido.
- ✅ **Pipeline de Scripting:** O uso de Reflection para mapeamento dinâmico de componentes scripts foi validado.
