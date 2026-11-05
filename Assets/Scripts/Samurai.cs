using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class Samurai : MonoBehaviour
{
    // Evita coleta duplicada por dois colliders/callbacks no mesmo frame.
    private HashSet<int> itensColetados = new HashSet<int>();
    static bool PegouPezinho;
    static float BonusBotinha;
    PhysicsMaterial2D MaterialSemAtrito;
    SpriteRenderer Renderer;
    bool trocandoCena;
    float reducaoCorrente;
    public Text UITextMoedas;
    public Text UITextEspadas;
    public Text UITextSkates;
    public Text UITextVidas;
    static int ContadorSkates = 0;
    static int ContadorEspadas = 0;
    static int ContadorMoedas = 0;
    static int ContadorVidas = 5;
    int ContadorPulos;
    static int TotalPulos;
    static readonly float[] RecargaPorProjetil = { .4f, .8f, 1.2f };
    static readonly float[] VelocidadePorProjetil = { 10, 7, 6 };
    const float VelocidadePulo = 6.5f;
    Rigidbody2D CorpoRigidoPersonagem;
    Animator AnimadorPersonagem;
    Collider2D ColisorPersonagem;
    // Nova partida/menu zeram o progresso; passagem Cena1 -> Cena2 conserva a coleta.
    public static void ReiniciarPartida()
    {
        PegouPezinho = false;
        BonusBotinha = 0;
        TotalPulos = 1;
        ContadorMoedas = 0;
        ContadorEspadas = 0;
        ContadorSkates = 0;
        ContadorVidas = 5;
    }

    void Start()
    {
        BonusBotinha = 0;
        if (modoMissao)
        {
            ContadorVidas = 5;
            ContadorMoedas = 0;
            ContadorEspadas = 0;
            ContadorSkates = 0;
            PegouPezinho = true;
        }
        MaterialSemAtrito = new PhysicsMaterial2D();
        MaterialSemAtrito.friction = 0f;
        Renderer = GetComponent<SpriteRenderer>();
        ContadorPulos = 0;
        TotalPulos = 1;
        CorpoRigidoPersonagem = GetComponent<Rigidbody2D>();
        AnimadorPersonagem = GetComponent<Animator>();
        ColisorPersonagem = GetComponent<Collider2D>();
        // Sem atrito no corpo: evita prender em laterais de caixas, grades e pisos.
        ColisorPersonagem.sharedMaterial = MaterialSemAtrito;
        // Aula 04x04: areas filhas identificadas pelas tags, mantendo os colliders originais.
        GameObject esquerda = GameObject.FindGameObjectWithTag("areaAtaqueEsquerda");
        GameObject direita = GameObject.FindGameObjectWithTag("areaAtaqueDireita");
        if (esquerda != null)
            areaKatanaEsquerda = esquerda.GetComponent<Collider2D>();
        if (direita != null)
            areaKatanaDireita = direita.GetComponent<Collider2D>();
        CorpoRigidoPersonagem.gravityScale = 1.2f;
        CorpoRigidoPersonagem.freezeRotation = true;
        UITextVidas.text = ContadorVidas.ToString();
        UITextEspadas.text = ContadorEspadas.ToString();
        UITextSkates.text = ContadorSkates.ToString();
        UITextMoedas.text = ContadorMoedas.ToString();
        CorpoRigidoPersonagem.velocity = Vector2.zero;
    }

    void Update()
    {
        if (trocandoCena)
            return;
        if (combateHabilitado || modoMissao)
            AtualizarCombate();
        if (modoMissao)
        {
            AtualizarMissao();
            if (missaoEncerrada)
                return;
        }
        MovimentoHorizontal();
        Pular();
        if (!modoMissao)
            VerificarVitoria();
    }

    void MovimentoHorizontal()
    {
        AplicarMovimento(Input.GetAxis("Horizontal"), Input.GetKey(KeyCode.LeftShift));
    }
    // Mantem a regra do backup; separa a leitura do teclado da aplicacao fisica.
    public void AplicarMovimento(float direcao, bool correr)
    {
        if (dashAtivo)
        {
            CorpoRigidoPersonagem.velocity = new Vector2(direcaoDash * velocidadeDash, CorpoRigidoPersonagem.velocity.y);
            return;
        }
        if (ataqueAtivo || defesaAtiva || missaoEncerrada)
            direcao = 0;
        correndo = correr;
        float velocidadeBase = (correr ? 5 : 3) + BonusBotinha;
        // O bonus de tempo soma ao permanente e expira sem apaga-lo (aula 03x07).
        float bonusTemporario = bonusRestante > 0 ? 2 : 0;
        float velocidadeMaxima = Mathf.Max(1, velocidadeBase + bonusTemporario - reducaoCorrente);
        float velocidadeX = velocidadeMaxima * direcao;
        if (ContadorPulos == 2)
            velocidadeX *= .8f;
        else if (ContadorPulos == 3)
            velocidadeX *= .6f;

        if (direcao != 0)
            Renderer.flipX = direcao < 0;

        CorpoRigidoPersonagem.velocity = new Vector2(velocidadeX, CorpoRigidoPersonagem.velocity.y);
        AnimadorPersonagem.SetFloat("VelocidadeX", Mathf.Abs(velocidadeX));
    }

    void Pular()
    {
        TentarPular(Input.GetButtonDown("Jump"));
    }

    public void TentarPular(bool apertou)
    {
        if (ataqueAtivo || defesaAtiva || dashAtivo || missaoEncerrada)
            return;
        if (!apertou || !PegouPezinho || ContadorPulos >= TotalPulos)
            return;

        ContadorPulos++;
        SequenciaPulo++;
        SomRonin.Tocar("pulo", transform.position);
        // Aula 02x06: calcular antes de montar e aplicar o vetor ao Rigidbody2D.
        float velocidadeY = VelocidadePulo;
        if (ContadorPulos == 2)
            velocidadeY *= .8f;
        else if (ContadorPulos == 3)
            velocidadeY *= .7f;

        CorpoRigidoPersonagem.velocity = new Vector2(CorpoRigidoPersonagem.velocity.x, velocidadeY);
    }

    void OnTriggerEnter2D(Collider2D objetoTriggerEnter)
    {
        if (ColisorPersonagem == null || !ColisorPersonagem.IsTouching(objetoTriggerEnter))
            return;
        ReceberColetavel(objetoTriggerEnter);
        VerificarObjetosDaMissao(objetoTriggerEnter);
    }

    void OnTriggerStay2D(Collider2D objetoTriggerStay)
    {
        if (ColisorPersonagem != null && ColisorPersonagem.IsTouching(objetoTriggerStay))
            VerificarObjetosDaMissao(objetoTriggerStay);
    }
    // Aulas 03x07/08: identificar o trigger pela tag antes de aplicar seu efeito.
    void VerificarObjetosDaMissao(Collider2D objeto)
    {
        if (!enabled || !modoMissao || missaoEncerrada)
            return;
        string tag = objeto.gameObject.tag;
        if (tag == "tipo1" && itensColetados.Add(objeto.gameObject.GetInstanceID()))
        {
            GanharBonusTemporario(8);
            Destroy(objeto.gameObject);
        }
        if (tag == "portal01" || tag == "portal02")
        {
            GameObject saida = GameObject.FindGameObjectWithTag(tag == "portal01" ? "portal02" : "portal01");
            if (saida != null)
                Teletransportar(saida.transform);
        }
        if (tag == "objetivo" && ConcluirMissao())
        {
            SpriteRenderer imagem = objeto.GetComponent<SpriteRenderer>();
            if (imagem != null)
                imagem.enabled = false;
        }
    }
    // Entrada comum para o personagem e para ColetavelRestaurado.
    public void ReceberColetavel(Collider2D objetoTriggerEnter)
    {
        if (!enabled || trocandoCena || missaoEncerrada || objetoTriggerEnter == null)
            return;
        string tipo = objetoTriggerEnter.gameObject.tag;
        if (tipo == "Limbo")
        {
            AlvoTemplo alvo = objetoTriggerEnter.GetComponentInParent<AlvoTemplo>();
            if (alvo != null)
            {
                if (!alvo.Derrotado)
                    ReceberDanoMissao(1, false);
                return;
            }
            print("OnTriggerEnter2D():" + tipo);
            ContadorVidas--;
            print("Vidas: " + ContadorVidas);
            UITextVidas.text = ContadorVidas.ToString();
            if (ContadorVidas <= 0)
            {
                ReiniciarPartida();
                TrocarCena("GameOver");
            }
            else
                TrocarCena(SceneManager.GetActiveScene().name);
            return;
        }
        if (tipo == "Caverna" && ContadorEspadas == 0 && ContadorMoedas >= 20)
        {
            print("OnTriggerEnter2D():" + tipo);
            TrocarCena("Cena2");
            return;
        }
        switch (tipo)
        {
            case "bonusPuloDuplo":
            case "bonusPuloTriplo":
            case "Botinha":
            case "Pezinho":
            case "Skate":
            case "Espadas":
            case "Espada2":
            case "Moedas":
            case "Corrente":
                AplicarColeta(objetoTriggerEnter.gameObject, tipo);
                break;
        }
    }

    void AplicarColeta(GameObject objeto, string tipo)
    {
        if (!itensColetados.Add(objeto.GetInstanceID()))
            return;

        SomRonin.Tocar("coleta", transform.position);
        print("OnTriggerEnter2D():" + tipo);
        switch (tipo)
        {
            case "bonusPuloDuplo":
            case "bonusPuloTriplo":
                PegouPezinho = true;
                TotalPulos = tipo == "bonusPuloDuplo" ? 2 : 3;
                print(TotalPulos == 2 ? "Pulo duplo habilitado!" : "Pulo triplo habilitado!");
                break;
            case "Botinha":
                if (BonusBotinha < 4)
                    BonusBotinha += 1f;
                print("Velocidade botinha: " + BonusBotinha);
                break;
            case "Pezinho":
                PegouPezinho = true;
                print("Pulo simples habilitado\nPoder de pulo: " + VelocidadePulo);
                break;
            case "Skate":
                ContadorSkates++;
                print("Skates: " + ContadorSkates);
                UITextSkates.text = ContadorSkates.ToString();
                break;
            case "Espadas":
                ContadorEspadas++;
                print("Espadas: " + ContadorEspadas);
                UITextEspadas.text = ContadorEspadas.ToString();
                break;
            case "Moedas":
                ContadorMoedas++;
                print("Moedas: " + ContadorMoedas);
                UITextMoedas.text = ContadorMoedas.ToString();
                break;
            case "Corrente":
                print("Movimentos Limitados!");
                PegouPezinho = true;
                TotalPulos = 1;
                reducaoCorrente = Mathf.Min(2, reducaoCorrente + 1);
                break;
        }

        Destroy(objeto);
        if (tipo == "Espada2")
            TrocarCena("Vitoria3");
    }

    void OnCollisionEnter2D(Collision2D objetoTocado)
    {
        string tag = objetoTocado.gameObject.tag;
        if (tag.Contains("semAtrito"))
        {
            print(" OnCollisionEnter2D():" + tag);
            objetoTocado.collider.sharedMaterial = MaterialSemAtrito;
        }
        if (tag.Contains("chao") || tag == "Plataforma" || tag == "mover")
        {
            foreach (ContactPoint2D contato in objetoTocado.contacts)
                if (contato.normal.y > .5f)
                    ContadorPulos = 0;
        }
    }
    // Aula 03x01: somente a caixa que esta encostando pode ser carregada pela tecla.
    void OnCollisionStay2D(Collision2D objetoPermaneceTocando)
    {
        if (objetoPermaneceTocando.gameObject.CompareTag("mover") && Input.GetKey(KeyCode.E))
            CarregarObjeto(objetoPermaneceTocando.gameObject.GetComponent<ObjetoTemplo>());
    }

    void OnCollisionExit2D(Collision2D objetoTocado)
    {
        string tag = objetoTocado.gameObject.tag;
        if (tag.Contains("semAtrito"))
        {
            print(" OnCollisionExit2D():" + tag);
            objetoTocado.collider.sharedMaterial = MaterialSemAtrito;
        }
    }
    // Aula 02x04: condicoes de progresso decidem qual cena deve ser carregada.
    void TrocarCena(string nome)
    {
        if (trocandoCena)
            return;
        trocandoCena = true;
        SceneManager.LoadScene(nome);
    }

    void VerificarVitoria()
    {
        if (trocandoCena)
            return;
        if (ContadorVidas <= 0)
        {
            ReiniciarPartida();
            TrocarCena("GameOver");
            return;
        }
        if (ContadorEspadas >= 2 && ContadorSkates >= 3)
        {
            ReiniciarPartida();
            TrocarCena("Vitoria1");
            return;
        }
        if (ContadorMoedas >= 50)
        {
            ReiniciarPartida();
            TrocarCena("Vitoria2");
        }
    }
    // Extensao da missao. Base didatica: jogos03x01/02/05/06/07/08 e 04x01/04.
    // Carry/plataforma/temporizador tambem identificados no IL da versao perdida.
    public bool modoMissao;
    public bool combateHabilitado;
    public float velocidadeDash = 12f, duracaoDash = .18f, intervaloDash = .7f;
    public bool dashAtivo
    {
        get
        {
            return Time.time < fimDash && !missaoEncerrada;
        }
    }

    public bool correndo
    {
        get;
        private set;
    }

    public int SequenciaPulo
    {
        get;
        private set;
    }

    public bool venceuMissao
    {
        get;
        private set;
    }
    float fimDash, proximoDash, direcaoDash, inicioAtaque;
    public bool JanelaKatana
    {
        get
        {
            return ataqueAtivo && Time.time >= inicioAtaque + 1f / 3f && Time.time < fimAtaque - .02f;
        }
    }

    public AlvoTemplo guardiaoFinal;
    public Text painelMissao;
    public GameObject[] projeteisMissao;
    public Collider2D areaKatanaEsquerda, areaKatanaDireita;
    public bool defesaAtiva
    {
        get;
        private set;
    }

    public bool ataqueAtivo
    {
        get;
        private set;
    }

    public bool ponteAtivada, portalUsado, missaoEncerrada;
    public int guardasDerrotados, tipoProjetil;
    public float bonusRestante
    {
        get;
        private set;
    }

    public int VidasMissao
    {
        get
        {
            return ContadorVidas;
        }
    }

    public ObjetoTemplo carregando
    {
        get;
        private set;
    }
    float fimAtaque, proximoAtaque, invulneravelAte, proximoPortal;
    bool PodeDisparar = true;
    Color corAntesDano;
    bool feedbackDano;
    readonly HashSet<int> alvosKatana = new HashSet<int>();
    void AtualizarMissao()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SairDaMissao();
            return;
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene("TravessiaDoTemplo");
            return;
        }
        if (missaoEncerrada)
            return;
        if (transform.position.y < -5)
        {
            ContadorVidas = 0;
            EncerrarMissao(false);
            if (missaoEncerrada)
                return;
            // Preserva a mensagem de derrota exibida neste frame.
        }
        if (Input.GetKeyUp(KeyCode.E))
            SoltarObjeto();
    }

    void AtualizarCombate()
    {
        ataqueAtivo = Time.time < fimAtaque && !missaoEncerrada;
        VerificarBonusAndar();
        if (feedbackDano && Renderer != null)
        {
            Renderer.color = Time.time < invulneravelAte && ((int)(Time.time * 12) % 2 == 0) ? new Color(1, .4f, .4f, .6f) : corAntesDano;
            if (Time.time >= invulneravelAte)
                feedbackDano = false;
        }
        DefinirDefesa(Input.GetKey(KeyCode.F));
        if (Input.GetKeyDown(KeyCode.Q))
            IniciarDash();
        if (Input.GetKeyDown(KeyCode.Z))
            AtacarKatana();
        if (Input.GetKeyDown(KeyCode.Alpha1))
            tipoProjetil = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2))
            tipoProjetil = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3))
            tipoProjetil = 2;
        if (Input.GetKey(KeyCode.X))
            DispararMissao();
        if (areaKatanaDireita != null)
            areaKatanaDireita.enabled = JanelaKatana && !Renderer.flipX;
        if (areaKatanaEsquerda != null)
            areaKatanaEsquerda.enabled = JanelaKatana && Renderer.flipX;
        tipoProjetil = Mathf.Clamp(tipoProjetil, 0, 2);
    }
    // Aula 03x07: o temporizador e atualizado em um metodo proprio.
    void VerificarBonusAndar()
    {
        if (bonusRestante <= 0)
            return;
        bonusRestante = Mathf.Max(0, bonusRestante - Time.deltaTime);
    }

    public bool IniciarDash()
    {
        if (!(combateHabilitado || modoMissao) || missaoEncerrada || ataqueAtivo || defesaAtiva || carregando != null || Time.time < proximoDash)
            return false;
        SomRonin.Tocar("dash", transform.position);
        direcaoDash = Renderer.flipX ? -1 : 1;
        fimDash = Time.time + Mathf.Max(.02f, duracaoDash);
        proximoDash = Time.time + Mathf.Max(duracaoDash, intervaloDash);
        CorpoRigidoPersonagem.velocity = new Vector2(direcaoDash * velocidadeDash, CorpoRigidoPersonagem.velocity.y);
        return true;
    }

    void LateUpdate()
    {
        if (!(combateHabilitado || modoMissao) || AnimadorPersonagem == null)
            return;
        if (defesaAtiva || ataqueAtivo || missaoEncerrada)
            CorpoRigidoPersonagem.velocity = new Vector2(0, CorpoRigidoPersonagem.velocity.y);
    }

    public void CarregarObjeto(ObjetoTemplo objeto)
    {
        if (!modoMissao || missaoEncerrada || ataqueAtivo || defesaAtiva || dashAtivo || carregando != null || objeto == null || !objeto.CompareTag("mover") || Vector2.Distance(transform.position, objeto.transform.position) > 1.8f)
            return;
        Rigidbody2D rb = objeto.GetComponent<Rigidbody2D>();
        Collider2D colisor = objeto.GetComponent<Collider2D>();
        if (rb == null || colisor == null)
            return;
        carregando = objeto;
        objeto.carregado = true;
        rb.velocity = Vector2.zero;
        rb.isKinematic = true;
        colisor.enabled = false;
        objeto.transform.SetParent(transform, true);
        objeto.transform.position = transform.position + Vector3.up * 1.5f;
    }

    public void SoltarObjeto()
    {
        if (carregando == null)
            return;
        carregando.transform.SetParent(null, true);
        carregando.transform.position = transform.position + Vector3.right * (Renderer.flipX ? -1.4f : 1.4f);
        carregando.carregado = false;
        carregando.GetComponent<Collider2D>().enabled = true;
        carregando.GetComponent<Rigidbody2D>().isKinematic = false;
        carregando = null;
    }

    public void GanharBonusTemporario(float segundos)
    {
        bonusRestante = Mathf.Max(0, segundos);
        SomRonin.Tocar("coleta", transform.position);
    }

    public bool AtacarKatana()
    {
        if (!(combateHabilitado || modoMissao) || missaoEncerrada || defesaAtiva || dashAtivo || carregando != null || Time.time < proximoAtaque)
            return false;
        inicioAtaque = Time.time;
        fimAtaque = Time.time + .5f;
        proximoAtaque = Time.time + .58f;
        ataqueAtivo = true;
        alvosKatana.Clear();
        SomRonin.Tocar("katana", transform.position);
        return true;
    }

    public void AcertarKatana(AlvoTemplo alvo)
    {
        if (JanelaKatana && alvo != null && alvosKatana.Add(alvo.GetInstanceID()))
            alvo.ReceberDano(1, this);
    }

    public GameObject DispararMissao()
    {
        if (!(combateHabilitado || modoMissao) || missaoEncerrada || ataqueAtivo || defesaAtiva || dashAtivo || carregando != null || !PodeDisparar)
            return null;
        int tipo = Mathf.Clamp(tipoProjetil, 0, 2);
        // Aula 04x01: encontra o modelo pela tag, cria uma copia e limita a cadencia com Invoke.
        GameObject modelo = GameObject.FindGameObjectWithTag("projetil" + (tipo + 1));
        if (modelo == null || modelo.GetComponent<ProjetilTemplo>() == null || modelo.GetComponent<Rigidbody2D>() == null)
            return null;
        PodeDisparar = false;
        Invoke("AtivarDisparo", RecargaPorProjetil[tipo]);
        float direcao = Renderer.flipX ? -1 : 1;
        GameObject tiro = Instantiate(modelo, transform.position + new Vector3(direcao * 0.9f, 0, 0), Quaternion.identity);
        tiro.GetComponent<ProjetilTemplo>().PrepararDisparo(this);
        SomRonin.Tocar(tipo == 2 ? "magia" : "tiro", transform.position);
        float velocidadeY = tipo == 2 ? 5 : 0;
        tiro.GetComponent<Rigidbody2D>().velocity = new Vector2(direcao * VelocidadePorProjetil[tipo], velocidadeY);
        return tiro;
    }

    void AtivarDisparo()
    {
        PodeDisparar = true;
    }

    public void DefinirDefesa(bool ativa)
    {
        defesaAtiva = ativa && !ataqueAtivo && !dashAtivo && carregando == null && !missaoEncerrada;
    }

    public bool ReceberDanoMissao(int dano, bool projetil)
    {
        if (!(combateHabilitado || modoMissao) || dano <= 0 || missaoEncerrada || Time.time < invulneravelAte || (projetil && defesaAtiva))
            return false;
        if (!feedbackDano && Renderer != null)
            corAntesDano = Renderer.color;
        feedbackDano = true;
        SomRonin.Tocar("dano", transform.position);
        ContadorVidas = Mathf.Max(0, ContadorVidas - dano);
        invulneravelAte = Time.time + 1.2f;
        if (UITextVidas != null)
            UITextVidas.text = ContadorVidas.ToString();
        if (ContadorVidas == 0 && modoMissao)
            EncerrarMissao(false);
        return true;
    }

    public void AtivarPonte()
    {
        ponteAtivada = true;
    }

    public void SairDaMissao()
    {
        // A missao libera o pulo desde o inicio; a fase antiga exige coletar Pezinho.
        // Limpa apenas ao voltar desta missao, sem interferir na passagem Cena1 -> Cena2.
        PegouPezinho = false;
        BonusBotinha = 0;
        ContadorVidas = 5;
        ContadorMoedas = 0;
        ContadorEspadas = 0;
        ContadorSkates = 0;
        SceneManager.LoadScene("FAses");
    }

    public bool Teletransportar(Transform destino)
    {
        if (!modoMissao || missaoEncerrada || destino == null || Time.time < proximoPortal || !ponteAtivada || guardasDerrotados < 3)
            return false;
        SoltarObjeto();
        proximoPortal = Time.time + 1;
        transform.SetParent(null, true);
        // Aula 03x08: sai a direita do portal02 e a esquerda do portal01, fora do trigger.
        float ladoSaida = destino.CompareTag("portal01") ? -1f : 1f;
        transform.position = destino.position + Vector3.right * (1.8f * ladoSaida);
        CorpoRigidoPersonagem.velocity = Vector2.zero;
        portalUsado = true;
        SomRonin.Tocar("portal", transform.position);
        return true;
    }

    public bool ConcluirMissao()
    {
        if (!modoMissao || missaoEncerrada || !ponteAtivada || !portalUsado || guardasDerrotados < 3 || (guardiaoFinal != null && !guardiaoFinal.Derrotado))
            return false;
        EncerrarMissao(true);
        return true;
    }

    public void VencerMago(AlvoTemplo chefe)
    {
        if (!modoMissao || missaoEncerrada || chefe == null || chefe != guardiaoFinal || !chefe.Derrotado)
            return;
        EncerrarMissao(true);
    }

    void EncerrarMissao(bool ganhou)
    {
        SoltarObjeto();
        missaoEncerrada = true;
        venceuMissao = ganhou;
        defesaAtiva = false;
        ataqueAtivo = false;
        if (feedbackDano && Renderer != null)
            Renderer.color = corAntesDano;
        feedbackDano = false;
        if (areaKatanaDireita != null)
            areaKatanaDireita.enabled = false;
        if (areaKatanaEsquerda != null)
            areaKatanaEsquerda.enabled = false;
        CorpoRigidoPersonagem.velocity = Vector2.zero;
        if (painelMissao != null)
            painelMissao.text = (ganhou ? "PERGAMINHO RECUPERADO! A rota para a cidade esta segura." : "O SAMURAI CAIU. Tente outra estrategia.") + "\nR: reiniciar missao   |   Esc: selecao de fases";
    }
}
