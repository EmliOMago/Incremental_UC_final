using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEngine.UI;

public class HUDManeger : MonoBehaviour
{
    public enum CategoriaMelhoria
    {
        Ativo = 0,
        Passivo = 1
    }

    [Serializable]
    public class DefinicaoMelhoria
    {
        [Header("Identificação")]
        [Tooltip("Número usado para montar as chaves automáticas da tabela: upgrade.{indice}.title / description / value")]
        public int indiceTabela;

        [Tooltip("Categoria da melhoria. Ativo influencia o clique. Passivo influencia o ganho automático.")]
        public CategoriaMelhoria categoria;

        [Header("Economia")]
        [Tooltip("Preço inicial da melhoria.")]
        public float precoBase = 10f;

        [Tooltip("Progressão do preço a cada compra.")]
        public float multiplicadorPreco = 1.15f;

        [Tooltip("Percentual multiplicativo aplicado por compra. Ex.: 0.5 = 50% por compra.")]
        public float percentualPorCompra = 0.5f;

        [Tooltip("Se verdadeiro, esta melhoria já começa liberada quando não houver save.")]
        public bool liberadaNoInicio;

        [Header("Localização opcional")]
        [Tooltip("Se vazio, usa fallback automático upgrade.{indice}.title")]
        public string chaveTitulo;

        [Tooltip("Se vazio, usa fallback automático upgrade.{indice}.description")]
        public string chaveDescricao;

        [Tooltip("Se vazio, usa fallback automático upgrade.{indice}.value")]
        public string chaveValor;

        [NonSerialized] public int quantidadeComprada;

        public string ObterChaveTitulo() => string.IsNullOrWhiteSpace(chaveTitulo) ? $"upgrade.{indiceTabela}.title" : chaveTitulo;
        public string ObterChaveDescricao() => string.IsNullOrWhiteSpace(chaveDescricao) ? $"upgrade.{indiceTabela}.description" : chaveDescricao;
        public string ObterChaveValor() => string.IsNullOrWhiteSpace(chaveValor) ? $"upgrade.{indiceTabela}.value" : chaveValor;

        public float ObterPrecoAtual()
        {
            float preco = precoBase * Mathf.Pow(multiplicadorPreco, quantidadeComprada);
            return Mathf.Max(precoBase, preco);
        }

        public float ObterPercentualTotal()
        {
            if (quantidadeComprada <= 0)
                return 0f;

            float multiplicador = Mathf.Pow(1f + Mathf.Max(0f, percentualPorCompra), quantidadeComprada);
            return (multiplicador - 1f) * 100f;
        }

        public float ObterMultiplicadorTotal()
        {
            if (quantidadeComprada <= 0)
                return 1f;

            return Mathf.Pow(1f + Mathf.Max(0f, percentualPorCompra), quantidadeComprada);
        }
    }

    [Serializable]
    public class EstadoMelhoriaSave
    {
        public int indice;
        public int quantidade;
    }

    [Serializable]
    public class SaveMelhoriasHUD
    {
        public int quantidadeDesbloqueadas;
        public float valorReferenciaDesbloqueio;
        public float maiorDinheiroAtingido;
        public List<EstadoMelhoriaSave> melhorias = new List<EstadoMelhoriaSave>();
    }

    [Serializable]
    public class ItemUpgradeUI
    {
        public GameObject instancia;
        public Button botaoComprar;
        public TextMeshProUGUI textoBotaoComprar;
        public TextMeshProUGUI textoTitulo;
        public TextMeshProUGUI textoDescricao;
        public TextMeshProUGUI textoValorAumentado;
        public TextMeshProUGUI textoPreco;
    }

    [Header("HUD")]
    public TextMeshProUGUI dinheiroText;

    [Header("Tela de carregamento")]
    public GameObject telaCarregamentoRaiz;
    public Image imagemTelaCarregamento;
    public Slider sliderCarregamento;

    [Header("Lista dinâmica de melhorias")]
    [Tooltip("Pai onde os prefabs de melhoria serão instanciados automaticamente.")]
    public Transform contentMelhorias;

    [Tooltip("Prefab do card de melhoria. O botão será encontrado automaticamente.")]
    public GameObject prefabMelhoria;

    [Header("Localização por CSV")]
    [Tooltip("CSV usado como fonte das traduções. Se vazio, tenta carregar por Resources.")]
    public TextAsset tabelaLocalizacaoCsv;

    [Tooltip("Nome do recurso em Resources. Se vazio, usa tentativas padrão.")]
    public string nomeTabelaLocalizacao = "HUD_Localization";

    [Tooltip("Força um idioma específico, ex.: pt-BR, en-US, id-ID. Se vazio, usa o idioma do sistema.")]
    public string codigoIdiomaForcado = "";

    [Tooltip("Melhorias adicionais criadas por você no inspector. As 6 melhorias fallback são adicionadas automaticamente.")]
    public List<DefinicaoMelhoria> melhoriasManuais = new List<DefinicaoMelhoria>();

    [Header("Economia base")]
    [Tooltip("Valor base ganho por drink manual antes dos multiplicadores.")]
    public float valorBaseDrinkManual = 1f;

    [Tooltip("Valor base por ciclo de cada compra passiva antes dos multiplicadores.")]
    public float valorBaseDrinkPassivoPorCompra = 1f;

    [Header("Desbloqueio progressivo")]
    [Tooltip("Multiplicador necessário para liberar a próxima melhoria.")]
    public float multiplicadorDesbloqueio = 100f;

    [Tooltip("Valor mínimo usado como primeira referência de desbloqueio.")]
    public float valorInicialReferenciaDesbloqueio = 1f;

    private readonly List<DefinicaoMelhoria> _melhorias = new List<DefinicaoMelhoria>();
    private readonly Dictionary<int, ItemUpgradeUI> _itensInstanciados = new Dictionary<int, ItemUpgradeUI>();
    private readonly Dictionary<string, string> _textosLocalizados = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _textosPtBr = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _indicesCabecalhoPorCodigo = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private static readonly Regex RegexMensagemTraducaoNaoEncontrada = new Regex(@"No translation found for '([^']+)' in .+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private bool _foiInicializado;
    private bool _estaInicializando;
    private bool _localizacaoCarregada;
    private string _codigoIdiomaAtual = "pt-BR";
    private int _quantidadeDesbloqueadas;
    private float _valorReferenciaDesbloqueio = 1f;
    private float _maiorDinheiroAtingido;

    private void OnEnable()
    {
        LevelManenger.OnDinheiroMudar += VerificarDesbloqueios;
        LevelManenger.OnDinheiroMudar += AtualizarHUDPorMudancaDeEstado;
    }

    private void OnDisable()
    {
        LevelManenger.OnDinheiroMudar -= VerificarDesbloqueios;
        LevelManenger.OnDinheiroMudar -= AtualizarHUDPorMudancaDeEstado;
    }

    private void Start()
    {
        GarantirInicializado();
    }

    public void PrepararTelaCarregamento()
    {
        GarantirReferenciasTelaCarregamento();

        if (telaCarregamentoRaiz != null)
            telaCarregamentoRaiz.SetActive(true);

        AtualizarProgressoCarregamento(0f);
    }

    public void AtualizarProgressoCarregamento(float progresso)
    {
        GarantirReferenciasTelaCarregamento();

        float progressoLimitado = Mathf.Clamp(progresso, 0f, 100f);

        if (imagemTelaCarregamento != null)
            imagemTelaCarregamento.fillAmount = progressoLimitado / 100f;

        if (sliderCarregamento != null)
        {
            sliderCarregamento.minValue = 0f;
            sliderCarregamento.maxValue = 100f;
            sliderCarregamento.value = progressoLimitado;
        }
    }

    public void ConcluirTelaCarregamento()
    {
        AtualizarProgressoCarregamento(100f);

        if (telaCarregamentoRaiz != null)
            telaCarregamentoRaiz.SetActive(false);
    }

    public void ReinicializarHUDDaCena()
    {
        string estadoAtual = _foiInicializado ? ExportarSaveHUD() : string.Empty;

        _foiInicializado = false;
        _estaInicializando = false;
        _localizacaoCarregada = false;
        _textosLocalizados.Clear();
        _textosPtBr.Clear();
        _indicesCabecalhoPorCodigo.Clear();
        _itensInstanciados.Clear();

        GarantirReferenciasTelaCarregamento();
        RemoverLocalizacaoLegadaDaHierarquia(contentMelhorias != null ? contentMelhorias.gameObject : gameObject);
        GarantirInicializado();

        if (!string.IsNullOrWhiteSpace(estadoAtual))
            AplicarSaveHUD(estadoAtual);
    }

    public void GarantirInicializado()
    {
        if (_foiInicializado || _estaInicializando)
            return;

        _estaInicializando = true;
        try
        {
            CriarListaFallbackSeNecessario();
            AplicarMelhoriasManuais();
            OrdenarMelhorias();
            InicializarEstadoPadraoSeNecessario();
            _foiInicializado = true;
            RecriarListaVisual();
        }
        finally
        {
            _estaInicializando = false;
        }
    }

    private void CriarListaFallbackSeNecessario()
    {
        _melhorias.Clear();

        _melhorias.Add(new DefinicaoMelhoria { indiceTabela = 0, categoria = CategoriaMelhoria.Ativo, precoBase = 10f, multiplicadorPreco = 1.18f, percentualPorCompra = 0.50f, liberadaNoInicio = true });
        _melhorias.Add(new DefinicaoMelhoria { indiceTabela = 1, categoria = CategoriaMelhoria.Passivo, precoBase = 100f, multiplicadorPreco = 1.20f, percentualPorCompra = 0.35f, liberadaNoInicio = false });
        _melhorias.Add(new DefinicaoMelhoria { indiceTabela = 2, categoria = CategoriaMelhoria.Ativo, precoBase = 1000f, multiplicadorPreco = 1.22f, percentualPorCompra = 0.75f, liberadaNoInicio = false });
        _melhorias.Add(new DefinicaoMelhoria { indiceTabela = 3, categoria = CategoriaMelhoria.Passivo, precoBase = 10000f, multiplicadorPreco = 1.25f, percentualPorCompra = 0.60f, liberadaNoInicio = false });
        _melhorias.Add(new DefinicaoMelhoria { indiceTabela = 4, categoria = CategoriaMelhoria.Ativo, precoBase = 100000f, multiplicadorPreco = 1.28f, percentualPorCompra = 1.00f, liberadaNoInicio = false });
        _melhorias.Add(new DefinicaoMelhoria { indiceTabela = 5, categoria = CategoriaMelhoria.Passivo, precoBase = 1000000f, multiplicadorPreco = 1.30f, percentualPorCompra = 0.90f, liberadaNoInicio = false });
    }

    private void AplicarMelhoriasManuais()
    {
        if (melhoriasManuais == null)
            return;

        foreach (DefinicaoMelhoria melhoriaManual in melhoriasManuais)
        {
            if (melhoriaManual == null)
                continue;

            bool jaExiste = false;
            for (int i = 0; i < _melhorias.Count; i++)
            {
                if (_melhorias[i].indiceTabela == melhoriaManual.indiceTabela)
                {
                    _melhorias[i] = melhoriaManual;
                    jaExiste = true;
                    break;
                }
            }

            if (!jaExiste)
                _melhorias.Add(melhoriaManual);
        }
    }

    private void OrdenarMelhorias()
    {
        _melhorias.Sort((a, b) => a.indiceTabela.CompareTo(b.indiceTabela));
    }

    private void InicializarEstadoPadraoSeNecessario()
    {
        _quantidadeDesbloqueadas = Mathf.Clamp(ContarLiberadasNoInicio(), 1, _melhorias.Count);
        _valorReferenciaDesbloqueio = Mathf.Max(1f, valorInicialReferenciaDesbloqueio);
        _maiorDinheiroAtingido = 0f;
    }

    private int ContarLiberadasNoInicio()
    {
        int quantidade = 0;
        for (int i = 0; i < _melhorias.Count; i++)
        {
            if (_melhorias[i].liberadaNoInicio)
                quantidade++;
        }

        return quantidade;
    }

    public void AplicarSaveHUD(string json)
    {
        GarantirInicializado();

        foreach (DefinicaoMelhoria melhoria in _melhorias)
            melhoria.quantidadeComprada = 0;

        if (!string.IsNullOrWhiteSpace(json))
        {
            SaveMelhoriasHUD save = JsonUtility.FromJson<SaveMelhoriasHUD>(json);
            if (save != null)
            {
                _quantidadeDesbloqueadas = Mathf.Clamp(save.quantidadeDesbloqueadas, 1, _melhorias.Count);
                _valorReferenciaDesbloqueio = Mathf.Max(1f, save.valorReferenciaDesbloqueio);
                _maiorDinheiroAtingido = Mathf.Max(0f, save.maiorDinheiroAtingido);

                if (save.melhorias != null)
                {
                    foreach (EstadoMelhoriaSave estado in save.melhorias)
                    {
                        DefinicaoMelhoria melhoria = ObterMelhoriaPorIndice(estado.indice);
                        if (melhoria != null)
                            melhoria.quantidadeComprada = Mathf.Max(0, estado.quantidade);
                    }
                }
            }
        }
        else
        {
            InicializarEstadoPadraoSeNecessario();
        }

        _quantidadeDesbloqueadas = Mathf.Clamp(_quantidadeDesbloqueadas, 1, _melhorias.Count);
        RecriarListaVisual();
        AtualizarTudo();
    }

    public void AplicarDadosEconomiaJogo(DadosEconomiaJogo dados)
    {
        GarantirInicializado();

        foreach (DefinicaoMelhoria melhoria in _melhorias)
            melhoria.quantidadeComprada = 0;

        if (dados != null)
        {
            _quantidadeDesbloqueadas = dados.quantidadeMelhoriasDesbloqueadas > 0
                ? dados.quantidadeMelhoriasDesbloqueadas
                : Mathf.Clamp(ContarLiberadasNoInicio(), 1, _melhorias.Count);
            _valorReferenciaDesbloqueio = Mathf.Max(1f, dados.valorReferenciaDesbloqueio > 0f ? dados.valorReferenciaDesbloqueio : valorInicialReferenciaDesbloqueio);
            _maiorDinheiroAtingido = Mathf.Max(0f, dados.maiorDinheiroAtingido);

            if (dados.melhorias != null)
            {
                foreach (DadosMelhoriaEconomia estado in dados.melhorias)
                {
                    if (estado == null)
                        continue;

                    DefinicaoMelhoria melhoria = ObterMelhoriaPorIndice(estado.indiceTabela);
                    if (melhoria != null)
                        melhoria.quantidadeComprada = Mathf.Max(0, estado.quantidadeComprada);
                }
            }
        }
        else
        {
            InicializarEstadoPadraoSeNecessario();
        }

        _quantidadeDesbloqueadas = Mathf.Clamp(Mathf.Max(_quantidadeDesbloqueadas, ContarLiberadasNoInicio()), 1, _melhorias.Count);
        RecriarListaVisual();
        AtualizarTudo();
    }

    public string ExportarSaveHUD()
    {
        GarantirInicializado();

        SaveMelhoriasHUD save = new SaveMelhoriasHUD
        {
            quantidadeDesbloqueadas = _quantidadeDesbloqueadas,
            valorReferenciaDesbloqueio = _valorReferenciaDesbloqueio,
            maiorDinheiroAtingido = _maiorDinheiroAtingido,
            melhorias = new List<EstadoMelhoriaSave>()
        };

        foreach (DefinicaoMelhoria melhoria in _melhorias)
        {
            save.melhorias.Add(new EstadoMelhoriaSave
            {
                indice = melhoria.indiceTabela,
                quantidade = melhoria.quantidadeComprada
            });
        }

        return JsonUtility.ToJson(save);
    }

    public DadosEconomiaJogo CapturarDadosEconomiaJogo()
    {
        GarantirInicializado();

        DadosEconomiaJogo dados = new DadosEconomiaJogo();
        dados.dinheiroAtual = GameDirector.instancia != null && GameDirector.instancia.levelManenger != null
            ? GameDirector.instancia.levelManenger.dinheiro
            : 0f;
        dados.quantidadeMelhoriasDesbloqueadas = _quantidadeDesbloqueadas;
        dados.valorReferenciaDesbloqueio = _valorReferenciaDesbloqueio;
        dados.maiorDinheiroAtingido = _maiorDinheiroAtingido;
        dados.totalComprasMelhorias = 0;

        foreach (DefinicaoMelhoria melhoria in _melhorias)
        {
            if (melhoria == null)
                continue;

            dados.totalComprasMelhorias += Mathf.Max(0, melhoria.quantidadeComprada);
            dados.melhorias.Add(new DadosMelhoriaEconomia
            {
                indiceTabela = melhoria.indiceTabela,
                idMelhoria = $"upgrade.{melhoria.indiceTabela}",
                categoria = melhoria.categoria.ToString(),
                quantidadeComprada = Mathf.Max(0, melhoria.quantidadeComprada),
                precoBase = melhoria.precoBase,
                precoAtual = melhoria.ObterPrecoAtual(),
                multiplicadorPreco = melhoria.multiplicadorPreco,
                percentualPorCompra = melhoria.percentualPorCompra,
                multiplicadorTotal = melhoria.ObterMultiplicadorTotal()
            });
        }

        return dados;
    }

    private void RecriarListaVisual()
    {
        _itensInstanciados.Clear();

        if (contentMelhorias == null || prefabMelhoria == null)
            return;

        for (int i = contentMelhorias.childCount - 1; i >= 0; i--)
            Destroy(contentMelhorias.GetChild(i).gameObject);

        for (int i = 0; i < _quantidadeDesbloqueadas && i < _melhorias.Count; i++)
            InstanciarItem(_melhorias[i]);
    }

    private void InstanciarItem(DefinicaoMelhoria melhoria)
    {
        GameObject instancia = Instantiate(prefabMelhoria, contentMelhorias);
        instancia.name = $"Melhoria_{melhoria.indiceTabela}";

        RemoverLocalizacaoLegadaDaHierarquia(instancia);
        AtualizacaoPrefabBindings bindings = GarantirBindingsDaInstancia(instancia);
        Button botao = bindings != null && bindings.BotaoComprar != null
            ? bindings.BotaoComprar
            : BuscarBotaoPorNome(instancia.transform, "Button") ?? instancia.GetComponentInChildren<Button>(true);
        TextMeshProUGUI textoTitulo = bindings != null && bindings.TextoTitulo != null
            ? bindings.TextoTitulo
            : BuscarTMPPorNome(instancia.transform, "Titulo");
        TextMeshProUGUI textoDescricao = bindings != null && bindings.TextoDescricao != null
            ? bindings.TextoDescricao
            : BuscarTMPPorNome(instancia.transform, "Descricao");
        TextMeshProUGUI textoValor = bindings != null && bindings.TextoGanho != null
            ? bindings.TextoGanho
            : BuscarTMPPorNome(instancia.transform, "Ganho");
        TextMeshProUGUI textoPreco = bindings != null && bindings.TextoPreco != null
            ? bindings.TextoPreco
            : BuscarTMPPorNome(instancia.transform, "Preco");
        TextMeshProUGUI textoBotao = bindings != null && bindings.TextoBotaoComprar != null
            ? bindings.TextoBotaoComprar
            : BuscarTextoBotao(instancia.transform, botao);

        ItemUpgradeUI item = new ItemUpgradeUI
        {
            instancia = instancia,
            botaoComprar = botao,
            textoBotaoComprar = textoBotao,
            textoTitulo = textoTitulo,
            textoDescricao = textoDescricao,
            textoValorAumentado = textoValor,
            textoPreco = textoPreco
        };

        if (item.botaoComprar != null)
        {
            item.botaoComprar.onClick.RemoveAllListeners();
            item.botaoComprar.onClick.AddListener(() => ComprarMelhoria(melhoria.indiceTabela));
        }

        _itensInstanciados[melhoria.indiceTabela] = item;
        AtualizarItemVisual(melhoria);
    }

    private AtualizacaoPrefabBindings GarantirBindingsDaInstancia(GameObject instancia)
    {
        if (instancia == null)
            return null;

        AtualizacaoPrefabBindings bindings = instancia.GetComponent<AtualizacaoPrefabBindings>();
        if (bindings == null)
            bindings = instancia.AddComponent<AtualizacaoPrefabBindings>();

        bindings.AtualizarListasDeComponentes();
        bindings.PreencherReferenciasPelosNomesPadrao();
        return bindings;
    }

    private void RemoverLocalizacaoLegadaDaHierarquia(GameObject raiz)
    {
        if (raiz == null)
            return;

        Component[] componentes = raiz.GetComponentsInChildren<Component>(true);
        foreach (Component componente in componentes)
        {
            if (componente == null)
                continue;

            if (!EhComponenteDeLocalizacaoLegada(componente))
                continue;

            if (componente is Behaviour behaviour)
                behaviour.enabled = false;

            Destroy(componente);
        }
    }

    private static bool EhComponenteDeLocalizacaoLegada(Component componente)
    {
        if (componente == null)
            return false;

        Type tipo = componente.GetType();
        if (tipo == null)
            return false;

        if (tipo == typeof(HUDManeger) || tipo == typeof(AtualizacaoPrefabBindings))
            return false;

        string nomeCompleto = tipo.FullName ?? string.Empty;
        string nome = tipo.Name ?? string.Empty;
        bool pertenceAoPacoteDeLocalizacao = false;

        bool pareceComponenteDeLocalizacao =
            nome.IndexOf("Localize", StringComparison.OrdinalIgnoreCase) >= 0 ||
            nome.IndexOf("Localized", StringComparison.OrdinalIgnoreCase) >= 0 ||
            nomeCompleto.IndexOf("Localization", StringComparison.OrdinalIgnoreCase) >= 0;

        return pertenceAoPacoteDeLocalizacao || pareceComponenteDeLocalizacao;
    }

    private static TextMeshProUGUI BuscarTMPPorNome(Transform raiz, string nome)
    {
        if (raiz == null || string.IsNullOrWhiteSpace(nome))
            return null;

        foreach (TextMeshProUGUI texto in raiz.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (string.Equals(texto.gameObject.name, nome, StringComparison.OrdinalIgnoreCase))
                return texto;
        }

        return null;
    }

    private static TextMeshProUGUI BuscarTextoBotao(Transform raiz, Button botao)
    {
        if (botao != null)
        {
            TextMeshProUGUI textoDireto = BuscarTMPPorNome(botao.transform, "Text (TMP)");
            if (textoDireto != null)
                return textoDireto;

            return botao.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        return BuscarTMPPorNome(raiz, "Text (TMP)");
    }

    private void GarantirReferenciasTelaCarregamento()
    {
        if (telaCarregamentoRaiz == null)
            telaCarregamentoRaiz = BuscarObjetoPorNomeNoHUD("PainelCarregamento");

        if (imagemTelaCarregamento == null && telaCarregamentoRaiz != null)
            imagemTelaCarregamento = BuscarComponenteFilhoPorNome<Image>(telaCarregamentoRaiz.transform, "Image");

        if (sliderCarregamento == null && telaCarregamentoRaiz != null)
            sliderCarregamento = BuscarComponenteFilhoPorNome<Slider>(telaCarregamentoRaiz.transform, "Slider");
    }

    private GameObject BuscarObjetoPorNomeNoHUD(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return null;

        foreach (Transform filho in GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(filho.gameObject.name, nome, StringComparison.OrdinalIgnoreCase))
                return filho.gameObject;
        }

        return null;
    }

    private static T BuscarComponenteFilhoPorNome<T>(Transform raiz, string nome) where T : Component
    {
        if (raiz == null || string.IsNullOrWhiteSpace(nome))
            return null;

        foreach (T componente in raiz.GetComponentsInChildren<T>(true))
        {
            if (componente != null && string.Equals(componente.gameObject.name, nome, StringComparison.OrdinalIgnoreCase))
                return componente;
        }

        return null;
    }

    private static Button BuscarBotaoPorNome(Transform raiz, string nome)
    {
        if (raiz == null || string.IsNullOrWhiteSpace(nome))
            return null;

        foreach (Button botao in raiz.GetComponentsInChildren<Button>(true))
        {
            if (string.Equals(botao.gameObject.name, nome, StringComparison.OrdinalIgnoreCase))
                return botao;
        }

        return null;
    }

    public void AtualizarTudo()
    {
        if (!_foiInicializado && !_estaInicializando)
            GarantirInicializado();

        AtualizarDinheiro();

        foreach (DefinicaoMelhoria melhoria in _melhorias)
            AtualizarItemVisualBase(melhoria);

        CarregarLocalizacaoCsvSeNecessario();

        foreach (DefinicaoMelhoria melhoria in _melhorias)
            AplicarLocalizacaoAoItem(melhoria);
    }

    public void AtualizarDinheiro()
    {
        if (dinheiroText == null || GameDirector.instancia == null || GameDirector.instancia.levelManenger == null)
            return;

        float dinheiroAtual = GameDirector.instancia.levelManenger.dinheiro;
        dinheiroText.text = FormatarMoeda(dinheiroAtual);
    }

    private void AtualizarHUDPorMudancaDeEstado()
    {
        if (!_foiInicializado || _estaInicializando)
            return;

        AtualizarDinheiro();
        AtualizarEstadoVisualDinamicoDosItens();
    }

    private void AtualizarEstadoVisualDinamicoDosItens()
    {
        foreach (DefinicaoMelhoria melhoria in _melhorias)
        {
            if (!_itensInstanciados.TryGetValue(melhoria.indiceTabela, out ItemUpgradeUI item))
                continue;

            if (item.textoPreco != null)
                DefinirTextoTMP(item.textoPreco, FormatarMoeda(melhoria.ObterPrecoAtual()));

            if (item.botaoComprar != null && GameDirector.instancia != null && GameDirector.instancia.levelManenger != null)
                item.botaoComprar.interactable = GameDirector.instancia.levelManenger.dinheiro >= melhoria.ObterPrecoAtual();
        }
    }

    private void VerificarDesbloqueios()
    {
        if (GameDirector.instancia == null || GameDirector.instancia.levelManenger == null)
            return;

        float dinheiroAtual = GameDirector.instancia.levelManenger.dinheiro;
        _maiorDinheiroAtingido = Mathf.Max(_maiorDinheiroAtingido, dinheiroAtual);
        bool houveDesbloqueio = false;

        while (_quantidadeDesbloqueadas < _melhorias.Count && _maiorDinheiroAtingido >= Mathf.Max(1f, _valorReferenciaDesbloqueio) * multiplicadorDesbloqueio)
        {
            _quantidadeDesbloqueadas++;
            _valorReferenciaDesbloqueio = Mathf.Max(1f, _maiorDinheiroAtingido);
            houveDesbloqueio = true;
        }

        if (houveDesbloqueio)
            RecriarListaVisual();
    }

    public bool ComprarMelhoria(int indiceTabela)
    {
        if (GameDirector.instancia == null || GameDirector.instancia.levelManenger == null)
            return false;

        DefinicaoMelhoria melhoria = ObterMelhoriaPorIndice(indiceTabela);
        if (melhoria == null)
            return false;

        float preco = melhoria.ObterPrecoAtual();
        LevelManenger level = GameDirector.instancia.levelManenger;
        if (level.dinheiro < preco)
            return false;

        level.AddDinheiro(-preco);
        melhoria.quantidadeComprada += 1;
        AtualizarTudo();
        return true;
    }

    public float ObterValorCliqueFinal()
    {
        return valorBaseDrinkManual * ObterMultiplicadorCategoria(CategoriaMelhoria.Ativo);
    }

    public float ObterValorPassivoFinalPorCiclo()
    {
        int comprasPassivas = ObterTotalComprasPorCategoria(CategoriaMelhoria.Passivo);
        if (comprasPassivas <= 0)
            return 0f;

        return valorBaseDrinkPassivoPorCompra * comprasPassivas * ObterMultiplicadorCategoria(CategoriaMelhoria.Passivo);
    }

    public float ObterMultiplicadorCategoria(CategoriaMelhoria categoria)
    {
        if (!_foiInicializado && !_estaInicializando)
            GarantirInicializado();

        float acumulado = 1f;
        foreach (DefinicaoMelhoria melhoria in _melhorias)
        {
            if (melhoria.categoria != categoria)
                continue;

            acumulado *= melhoria.ObterMultiplicadorTotal();
        }

        return Mathf.Max(1f, acumulado);
    }

    public int ObterTotalComprasPorCategoria(CategoriaMelhoria categoria)
    {
        if (!_foiInicializado && !_estaInicializando)
            GarantirInicializado();

        int total = 0;
        foreach (DefinicaoMelhoria melhoria in _melhorias)
        {
            if (melhoria.categoria == categoria)
                total += melhoria.quantidadeComprada;
        }

        return total;
    }

    private DefinicaoMelhoria ObterMelhoriaPorIndice(int indiceTabela)
    {
        for (int i = 0; i < _melhorias.Count; i++)
        {
            if (_melhorias[i].indiceTabela == indiceTabela)
                return _melhorias[i];
        }

        return null;
    }

    private void AtualizarItemVisual(DefinicaoMelhoria melhoria)
    {
        AtualizarItemVisualBase(melhoria);
        CarregarLocalizacaoCsvSeNecessario();
        AplicarLocalizacaoAoItem(melhoria);
    }

    private void AtualizarItemVisualBase(DefinicaoMelhoria melhoria)
    {
        if (!_itensInstanciados.TryGetValue(melhoria.indiceTabela, out ItemUpgradeUI item))
            return;

        DefinirTextoTMP(item.textoTitulo, ObterFallbackTitulo(melhoria));
        DefinirTextoTMP(item.textoDescricao, FormatarFallback(ObterFallbackDescricao(melhoria), FormatarPercentual(melhoria.ObterPercentualTotal())));
        DefinirTextoTMP(item.textoValorAumentado, FormatarFallback(ObterFallbackValor(melhoria), FormatarMoeda(melhoria.categoria == CategoriaMelhoria.Ativo ? ObterValorCliqueFinal() : ObterValorPassivoFinalPorCiclo())));
        DefinirTextoTMP(item.textoPreco, FormatarMoeda(melhoria.ObterPrecoAtual()));
        DefinirTextoTMP(item.textoBotaoComprar, "Adquirir");

        if (item.botaoComprar != null && GameDirector.instancia != null && GameDirector.instancia.levelManenger != null)
            item.botaoComprar.interactable = GameDirector.instancia.levelManenger.dinheiro >= melhoria.ObterPrecoAtual();
    }

    private void AplicarLocalizacaoAoItem(DefinicaoMelhoria melhoria)
    {
        if (!_itensInstanciados.TryGetValue(melhoria.indiceTabela, out ItemUpgradeUI item))
            return;

        string titulo = ObterTextoLocalizado(melhoria.ObterChaveTitulo(), ObterFallbackTitulo(melhoria));
        string descricao = ObterTextoLocalizado(
            melhoria.ObterChaveDescricao(),
            ObterFallbackDescricao(melhoria),
            FormatarPercentual(melhoria.ObterPercentualTotal()));
        string valor = ObterTextoLocalizado(
            melhoria.ObterChaveValor(),
            ObterFallbackValor(melhoria),
            FormatarMoeda(melhoria.categoria == CategoriaMelhoria.Ativo ? ObterValorCliqueFinal() : ObterValorPassivoFinalPorCiclo()));
        string textoBotao = ObterTextoLocalizado("upgrade.acquire", "Adquirir");

        DefinirTextoTMP(item.textoTitulo, titulo);
        DefinirTextoTMP(item.textoDescricao, descricao);
        DefinirTextoTMP(item.textoValorAumentado, valor);
        DefinirTextoTMP(item.textoBotaoComprar, textoBotao);
    }

    private static void DefinirTextoTMP(TextMeshProUGUI texto, string conteudo)
    {
        if (texto == null)
            return;

        string valorFinal = conteudo ?? string.Empty;
        if (!string.Equals(texto.text, valorFinal, StringComparison.Ordinal))
            texto.text = valorFinal;
    }

    private string ObterTextoLocalizado(string chave, string fallback, params object[] argumentos)
    {
        CarregarLocalizacaoCsvSeNecessario();

        string chaveNormalizada = NormalizarChaveLocalizacao(chave);
        string textoEspecial = ResolverTextoPorMensagemOuChave(fallback, argumentos);
        if (!string.IsNullOrWhiteSpace(textoEspecial))
            return textoEspecial;

        if (!string.IsNullOrWhiteSpace(chaveNormalizada))
        {
            if (_textosLocalizados.TryGetValue(chaveNormalizada, out string textoIdiomaAtual) && !string.IsNullOrWhiteSpace(textoIdiomaAtual))
                return FormatarFallback(textoIdiomaAtual, argumentos);

            if (_textosPtBr.TryGetValue(chaveNormalizada, out string textoPtBr) && !string.IsNullOrWhiteSpace(textoPtBr))
                return FormatarFallback(textoPtBr, argumentos);
        }

        return FormatarFallback(fallback, argumentos);
    }

    private string FormatarFallback(string textoBase, params object[] argumentos)
    {
        if (string.IsNullOrEmpty(textoBase))
            return string.Empty;

        if (argumentos == null || argumentos.Length == 0)
            return textoBase;

        try
        {
            return string.Format(textoBase, argumentos);
        }
        catch
        {
            return textoBase;
        }
    }

    private string ResolverTextoPorMensagemOuChave(string texto, params object[] argumentos)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return null;

        string textoLimpo = LimparCampoCsv(texto).Trim();
        if (string.IsNullOrWhiteSpace(textoLimpo))
            return null;

        Match match = RegexMensagemTraducaoNaoEncontrada.Match(textoLimpo);
        if (match.Success)
        {
            string chaveCapturada = NormalizarChaveLocalizacao(match.Groups[1].Value);
            if (TryObterTextoDaTabela(chaveCapturada, out string textoTabela))
                return FormatarFallback(textoTabela, argumentos);
        }

        string chaveDireta = NormalizarChaveLocalizacao(textoLimpo);
        if (!string.IsNullOrWhiteSpace(chaveDireta) && TryObterTextoDaTabela(chaveDireta, out string textoPorChave))
            return FormatarFallback(textoPorChave, argumentos);

        return null;
    }

    private bool TryObterTextoDaTabela(string chave, out string texto)
    {
        texto = null;
        if (string.IsNullOrWhiteSpace(chave))
            return false;

        if (_textosLocalizados.TryGetValue(chave, out texto) && !string.IsNullOrWhiteSpace(texto))
            return true;

        if (_textosPtBr.TryGetValue(chave, out texto) && !string.IsNullOrWhiteSpace(texto))
            return true;

        texto = null;
        return false;
    }

    private static string LimparCampoCsv(string valor)
    {
        if (string.IsNullOrEmpty(valor))
            return string.Empty;

        return valor.Trim().TrimStart('﻿');
    }

    private static string NormalizarChaveLocalizacao(string chave)
    {
        if (string.IsNullOrWhiteSpace(chave))
            return string.Empty;

        string normalizada = LimparCampoCsv(chave).Trim();
        normalizada = normalizada.Replace("upagrade.", "upgrade.");
        normalizada = normalizada.Replace("upagrade", "upgrade");
        return normalizada;
    }

    private void CarregarLocalizacaoCsvSeNecessario()
    {
        string codigoDesejado = ObterCodigoIdiomaDesejado();
        if (_localizacaoCarregada && string.Equals(_codigoIdiomaAtual, codigoDesejado, StringComparison.OrdinalIgnoreCase))
            return;

        _codigoIdiomaAtual = codigoDesejado;
        _textosLocalizados.Clear();
        _textosPtBr.Clear();
        _indicesCabecalhoPorCodigo.Clear();

        TextAsset csv = ObterCsvLocalizacao();
        if (csv == null || string.IsNullOrWhiteSpace(csv.text))
        {
            _localizacaoCarregada = true;
            return;
        }

        List<List<string>> linhas = ParseCsv(csv.text);
        if (linhas.Count == 0)
        {
            _localizacaoCarregada = true;
            return;
        }

        List<string> cabecalho = linhas[0];
        for (int i = 0; i < cabecalho.Count; i++)
        {
            cabecalho[i] = LimparCampoCsv(cabecalho[i]);
            string codigo = ExtrairCodigoCabecalho(cabecalho[i]);
            if (!string.IsNullOrWhiteSpace(codigo) && !_indicesCabecalhoPorCodigo.ContainsKey(codigo))
                _indicesCabecalhoPorCodigo.Add(codigo, i);
        }

        int indiceKey = cabecalho.FindIndex(c => string.Equals(LimparCampoCsv(c), "Key", StringComparison.OrdinalIgnoreCase));
        int indiceIdioma = ObterIndiceIdioma(cabecalho, _codigoIdiomaAtual);
        int indicePtBr = ObterIndiceIdioma(cabecalho, "pt-BR");
        if (indiceKey < 0 || (indiceIdioma < 0 && indicePtBr < 0))
        {
            _localizacaoCarregada = true;
            return;
        }

        for (int i = 1; i < linhas.Count; i++)
        {
            List<string> linha = linhas[i];
            if (linha.Count <= indiceKey)
                continue;

            string chave = NormalizarChaveLocalizacao(LimparCampoCsv(linha[indiceKey]));
            if (string.IsNullOrWhiteSpace(chave))
                continue;

            string valorIdiomaAtual = indiceIdioma >= 0 && indiceIdioma < linha.Count ? LimparCampoCsv(linha[indiceIdioma]) : string.Empty;
            string valorPtBr = indicePtBr >= 0 && indicePtBr < linha.Count ? LimparCampoCsv(linha[indicePtBr]) : string.Empty;

            if (!_textosLocalizados.ContainsKey(chave))
                _textosLocalizados.Add(chave, valorIdiomaAtual);

            if (!_textosPtBr.ContainsKey(chave))
                _textosPtBr.Add(chave, valorPtBr);
        }

        _localizacaoCarregada = true;
    }

    private TextAsset ObterCsvLocalizacao()
    {
        if (tabelaLocalizacaoCsv != null)
            return tabelaLocalizacaoCsv;

        string[] tentativas =
        {
            nomeTabelaLocalizacao,
            "HUD_Localization",
            "HUD Localization"
        };

        foreach (string tentativa in tentativas)
        {
            if (string.IsNullOrWhiteSpace(tentativa))
                continue;

            TextAsset encontrado = Resources.Load<TextAsset>(tentativa);
            if (encontrado != null)
                return encontrado;
        }

        return null;
    }

    private int ObterIndiceIdioma(List<string> cabecalho, string codigoDesejado)
    {
        if (!string.IsNullOrWhiteSpace(codigoDesejado) && _indicesCabecalhoPorCodigo.TryGetValue(codigoDesejado, out int indice))
            return indice;

        if (_indicesCabecalhoPorCodigo.TryGetValue("pt-BR", out indice))
            return indice;

        if (_indicesCabecalhoPorCodigo.TryGetValue("en-US", out indice))
            return indice;

        for (int i = 0; i < cabecalho.Count; i++)
        {
            string nomeColuna = LimparCampoCsv(cabecalho[i]);
            if (!string.Equals(nomeColuna, "Key", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(nomeColuna, "Id", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(nomeColuna, "Shared Comments", StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private string ObterCodigoIdiomaDesejado()
    {
        if (!string.IsNullOrWhiteSpace(codigoIdiomaForcado))
            return codigoIdiomaForcado.Trim();

        switch (Application.systemLanguage)
        {
            case SystemLanguage.Portuguese: return "pt-BR";
            case SystemLanguage.English: return "en-US";
            case SystemLanguage.German: return "de-DE";
            case SystemLanguage.Italian: return "it-IT";
            case SystemLanguage.French: return "fr-FR";
            case SystemLanguage.Spanish: return "es-ES";
            case SystemLanguage.Russian: return "ru-RU";
            case SystemLanguage.ChineseSimplified: return "zh-Hans";
            case SystemLanguage.Hindi: return "hi-IN";
            case SystemLanguage.Indonesian: return "id-ID";
            default: return "pt-BR";
        }
    }

    private static string ExtrairCodigoCabecalho(string cabecalho)
    {
        cabecalho = LimparCampoCsv(cabecalho);
        if (string.IsNullOrWhiteSpace(cabecalho))
            return string.Empty;

        int inicio = cabecalho.LastIndexOf('(');
        int fim = cabecalho.LastIndexOf(')');
        if (inicio >= 0 && fim > inicio)
            return cabecalho.Substring(inicio + 1, fim - inicio - 1).Trim();

        return cabecalho.Trim();
    }

    private static List<List<string>> ParseCsv(string texto)
    {
        List<List<string>> linhas = new List<List<string>>();
        List<string> linhaAtual = new List<string>();
        System.Text.StringBuilder campoAtual = new System.Text.StringBuilder();
        bool emAspas = false;

        for (int i = 0; i < texto.Length; i++)
        {
            char c = texto[i];

            if (c == '"')
            {
                if (emAspas && i + 1 < texto.Length && texto[i + 1] == '"')
                {
                    campoAtual.Append('"');
                    i++;
                }
                else
                {
                    emAspas = !emAspas;
                }
            }
            else if (c == ',' && !emAspas)
            {
                linhaAtual.Add(campoAtual.ToString());
                campoAtual.Length = 0;
            }
            else if ((c == '\r' || c == '\n') && !emAspas)
            {
                if (c == '\r' && i + 1 < texto.Length && texto[i + 1] == '\n')
                    i++;

                linhaAtual.Add(campoAtual.ToString());
                campoAtual.Length = 0;
                linhas.Add(linhaAtual);
                linhaAtual = new List<string>();
            }
            else
            {
                campoAtual.Append(c);
            }
        }

        if (campoAtual.Length > 0 || linhaAtual.Count > 0)
        {
            linhaAtual.Add(campoAtual.ToString());
            linhas.Add(linhaAtual);
        }

        return linhas;
    }

    private string ObterFallbackTitulo(DefinicaoMelhoria melhoria)
    {
        switch (melhoria.indiceTabela)
        {
            case 0: return "Canudo Turbo";
            case 1: return "Máquina de Drinks";
            case 2: return "Receita da Casa";
            case 3: return "Entrega Gelada";
            case 4: return "Cardápio VIP";
            case 5: return "Franquia Automática";
            default: return $"Melhoria {melhoria.indiceTabela}";
        }
    }

    private string ObterFallbackDescricao(DefinicaoMelhoria melhoria)
    {
        switch (melhoria.indiceTabela)
        {
            case 0: return "Aumenta o lucro por drink manual em {0}.";
            case 1: return "Multiplica o lucro automático de drinks em {0}.";
            case 2: return "Multiplica os drinks vendidos no clique em {0}.";
            case 3: return "Multiplica os drinks automáticos em {0}.";
            case 4: return "Valoriza cada drink manual em {0}.";
            case 5: return "Expande a renda automática de drinks em {0}.";
            default:
                return melhoria.categoria == CategoriaMelhoria.Ativo
                    ? "Multiplica os drinks manuais em {0}."
                    : "Multiplica os drinks automáticos em {0}.";
        }
    }

    private string ObterFallbackValor(DefinicaoMelhoria melhoria)
    {
        return melhoria.categoria == CategoriaMelhoria.Ativo
            ? "Drink manual rende {0}"
            : "Auto drinks rendem {0}";
    }

    public static string FormatarMoeda(float valor)
    {
        return "$ " + FormatarComPrefixo(valor, 2);
    }

    public static string FormatarPercentual(float valor)
    {
        return FormatarComPrefixo(valor, 2) + "%";
    }

    public static string FormatarComPrefixo(float valor, int casasDecimais)
    {
        string[] prefixos = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };
        double valorAbsoluto = Math.Abs(valor);
        double valorFormatado = valor;
        int indice = 0;

        while (valorAbsoluto >= 1000d && indice < prefixos.Length - 1)
        {
            valorFormatado /= 1000d;
            valorAbsoluto /= 1000d;
            indice++;
        }

        return valorFormatado.ToString($"F{casasDecimais}") + prefixos[indice];
    }
}
