using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
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
        public TextMeshProUGUI textoTitulo;
        public TextMeshProUGUI textoDescricao;
        public TextMeshProUGUI textoValorAumentado;
        public TextMeshProUGUI textoPreco;
        public TextMeshProUGUI textoBotao;
    }

    [Serializable]
    private class LinhaLocalizacaoCsv
    {
        public string chave;
        public readonly Dictionary<string, string> valores = new Dictionary<string, string>();
    }

    [Header("HUD")]
    public TextMeshProUGUI dinheiroText;

    [Header("Lista dinâmica de melhorias")]
    [Tooltip("Pai onde os prefabs de melhoria serão instanciados automaticamente.")]
    public Transform contentMelhorias;

    [Tooltip("Prefab do card de melhoria. O botão será encontrado automaticamente.")]
    public GameObject prefabMelhoria;

    [Tooltip("Tabela de localização usada para os textos das melhorias.")]
    public string nomeTabelaLocalizacao = "HUD_Upgrades";

    [Tooltip("Melhorias adicionais criadas por você no inspector. As 6 melhorias fallback são adicionadas automaticamente.")]
    public List<DefinicaoMelhoria> melhoriasManuais = new List<DefinicaoMelhoria>();

    [Header("Localização CSV (fallback)")]
    [Tooltip("Se vazio, tenta carregar automaticamente Resources/HUD_Localization.csv")]
    public TextAsset tabelaLocalizacaoCsv;

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
    private readonly Dictionary<string, LinhaLocalizacaoCsv> _localizacaoCsv = new Dictionary<string, LinhaLocalizacaoCsv>(StringComparer.OrdinalIgnoreCase);

    private bool _foiInicializado;
    private bool _localizacaoCsvCarregada;
    private int _quantidadeDesbloqueadas;
    private float _valorReferenciaDesbloqueio = 1f;
    private float _maiorDinheiroAtingido;
    private GameObject _templateMelhoriaCena;
    private bool _recriandoLista;
    private Coroutine _rotinaAguardarLocalizacao;

    private void OnEnable()
    {
        LevelManenger.OnDinheiroMudar += AtualizarDinheiro;
        LevelManenger.OnDinheiroMudar += VerificarDesbloqueios;
        LocalizationSettings.SelectedLocaleChanged += AoMudarLocale;
        IniciarAguardarLocalizacao();
    }

    private void OnDisable()
    {
        LevelManenger.OnDinheiroMudar -= AtualizarDinheiro;
        LevelManenger.OnDinheiroMudar -= VerificarDesbloqueios;
        LocalizationSettings.SelectedLocaleChanged -= AoMudarLocale;

        if (_rotinaAguardarLocalizacao != null)
        {
            StopCoroutine(_rotinaAguardarLocalizacao);
            _rotinaAguardarLocalizacao = null;
        }
    }

    private void Start()
    {
        GarantirInicializado();
        AtualizarTudo();
    }

    private void IniciarAguardarLocalizacao()
    {
        if (!isActiveAndEnabled || _rotinaAguardarLocalizacao != null)
            return;

        _rotinaAguardarLocalizacao = StartCoroutine(RotinaAguardarLocalizacao());
    }

    private IEnumerator RotinaAguardarLocalizacao()
    {
        var initOp = LocalizationSettings.InitializationOperation;
        if (initOp.IsValid() && !initOp.IsDone)
            yield return initOp;

        _rotinaAguardarLocalizacao = null;
        AtualizarTudo();
    }

    private void AoMudarLocale(UnityEngine.Localization.Locale _)
    {
        AtualizarTudo();
    }

    public void GarantirInicializado()
    {
        if (_foiInicializado)
            return;

        ResolverReferenciasAutomaticas();
        CarregarLocalizacaoCsvSeNecessario();
        CriarListaFallbackSeNecessario();
        AplicarMelhoriasManuais();
        OrdenarMelhorias();
        InicializarEstadoPadraoSeNecessario();
        RecriarListaVisual();
        _foiInicializado = true;
    }

    public void ReinicializarHUDDaCena()
    {
        _foiInicializado = false;
        _templateMelhoriaCena = null;
        _recriandoLista = false;
        _itensInstanciados.Clear();
        GarantirInicializado();
        AtualizarTudo();
    }

    private void ResolverReferenciasAutomaticas()
    {
        if (contentMelhorias == null)
        {
            GameObject contentObject = GameObject.Find("Content");
            if (contentObject != null)
                contentMelhorias = contentObject.transform;
        }

        if (contentMelhorias != null && prefabMelhoria == null && _templateMelhoriaCena == null)
        {
            for (int i = 0; i < contentMelhorias.childCount; i++)
            {
                Transform filho = contentMelhorias.GetChild(i);
                if (filho.GetComponentInChildren<Button>(true) != null && filho.GetComponentInChildren<TextMeshProUGUI>(true) != null)
                {
                    _templateMelhoriaCena = filho.gameObject;
                    _templateMelhoriaCena.SetActive(false);
                    break;
                }
            }
        }
    }

    private void CarregarLocalizacaoCsvSeNecessario()
    {
        if (_localizacaoCsvCarregada)
            return;

        _localizacaoCsvCarregada = true;
        _localizacaoCsv.Clear();

        if (tabelaLocalizacaoCsv == null)
            tabelaLocalizacaoCsv = Resources.Load<TextAsset>("HUD_Localization");

        if (tabelaLocalizacaoCsv == null || string.IsNullOrWhiteSpace(tabelaLocalizacaoCsv.text))
            return;

        string texto = tabelaLocalizacaoCsv.text.Replace("\r\n", "\n").Replace('\r', '\n');
        string[] linhas = texto.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (linhas.Length <= 1)
            return;

        string[] cabecalhos = QuebrarCsv(linhas[0]);
        for (int i = 1; i < linhas.Length; i++)
        {
            string linhaBruta = linhas[i];
            if (string.IsNullOrWhiteSpace(linhaBruta))
                continue;

            string[] colunas = QuebrarCsv(linhaBruta);
            if (colunas.Length == 0 || string.IsNullOrWhiteSpace(colunas[0]))
                continue;

            LinhaLocalizacaoCsv linha = new LinhaLocalizacaoCsv { chave = colunas[0] };
            for (int c = 1; c < cabecalhos.Length && c < colunas.Length; c++)
            {
                string cabecalho = cabecalhos[c].Trim();
                if (string.IsNullOrWhiteSpace(cabecalho))
                    continue;

                linha.valores[cabecalho] = colunas[c];
            }

            _localizacaoCsv[linha.chave] = linha;
        }
    }

    private string[] QuebrarCsv(string linha)
    {
        List<string> valores = new List<string>();
        bool emAspas = false;
        System.Text.StringBuilder atual = new System.Text.StringBuilder();

        for (int i = 0; i < linha.Length; i++)
        {
            char caractere = linha[i];
            if (caractere == '"')
            {
                if (emAspas && i + 1 < linha.Length && linha[i + 1] == '"')
                {
                    atual.Append('"');
                    i++;
                }
                else
                {
                    emAspas = !emAspas;
                }
            }
            else if (caractere == ',' && !emAspas)
            {
                valores.Add(atual.ToString());
                atual.Length = 0;
            }
            else
            {
                atual.Append(caractere);
            }
        }

        valores.Add(atual.ToString());
        return valores.ToArray();
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

    private void RecriarListaVisual()
    {
        if (_recriandoLista)
            return;

        _recriandoLista = true;
        _itensInstanciados.Clear();
        ResolverReferenciasAutomaticas();

        if (contentMelhorias == null)
        {
            _recriandoLista = false;
            return;
        }

        for (int i = contentMelhorias.childCount - 1; i >= 0; i--)
        {
            GameObject filho = contentMelhorias.GetChild(i).gameObject;
            if (_templateMelhoriaCena != null && filho == _templateMelhoriaCena)
                continue;

            Destroy(filho);
        }

        int quantidade = Mathf.Min(_quantidadeDesbloqueadas, _melhorias.Count);
        for (int i = 0; i < quantidade; i++)
            InstanciarItem(_melhorias[i]);

        _recriandoLista = false;
    }

    private void InstanciarItem(DefinicaoMelhoria melhoria)
    {
        GameObject origem = prefabMelhoria != null ? prefabMelhoria : _templateMelhoriaCena;
        if (origem == null)
            return;

        GameObject instancia;
        if (_templateMelhoriaCena != null && origem == _templateMelhoriaCena)
            instancia = Instantiate(_templateMelhoriaCena, contentMelhorias);
        else
            instancia = Instantiate(prefabMelhoria, contentMelhorias);

        instancia.name = $"Melhoria_{melhoria.indiceTabela}";
        instancia.SetActive(true);

        ItemUpgradeUI item = new ItemUpgradeUI
        {
            instancia = instancia,
            botaoComprar = instancia.GetComponentInChildren<Button>(true),
            textoTitulo = EncontrarTMP(instancia.transform, "Titulo"),
            textoDescricao = EncontrarTMP(instancia.transform, "Descricao"),
            textoValorAumentado = EncontrarTMP(instancia.transform, "Ganho"),
            textoPreco = EncontrarTMP(instancia.transform, "Preco"),
            textoBotao = EncontrarTMP(instancia.transform, "Text (TMP)")
        };

        if (item.botaoComprar != null)
        {
            item.botaoComprar.onClick.RemoveAllListeners();
            item.botaoComprar.onClick.AddListener(() => ComprarMelhoria(melhoria.indiceTabela));
        }

        _itensInstanciados[melhoria.indiceTabela] = item;
        AtualizarItemVisual(melhoria);
    }

    private TextMeshProUGUI EncontrarTMP(Transform raiz, string nome)
    {
        if (raiz == null)
            return null;

        TextMeshProUGUI[] textos = raiz.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI texto in textos)
        {
            if (texto != null && texto.name.Equals(nome, StringComparison.OrdinalIgnoreCase))
                return texto;
        }

        return null;
    }

    public void AtualizarTudo()
    {
        GarantirInicializado();
        AtualizarDinheiro();

        foreach (DefinicaoMelhoria melhoria in _melhorias)
            AtualizarItemVisual(melhoria);
    }

    public void AtualizarDinheiro()
    {
        if (dinheiroText == null || GameDirector.instancia == null || GameDirector.instancia.levelManenger == null)
            return;

        float dinheiroAtual = GameDirector.instancia.levelManenger.dinheiro;
        dinheiroText.text = FormatarMoeda(dinheiroAtual);
    }

    private void VerificarDesbloqueios()
    {
        if (GameDirector.instancia == null || GameDirector.instancia.levelManenger == null)
            return;

        float dinheiroAtual = GameDirector.instancia.levelManenger.dinheiro;
        _maiorDinheiroAtingido = Mathf.Max(_maiorDinheiroAtingido, dinheiroAtual);

        bool desbloqueou = false;
        while (_quantidadeDesbloqueadas < _melhorias.Count && _maiorDinheiroAtingido >= Mathf.Max(1f, _valorReferenciaDesbloqueio) * multiplicadorDesbloqueio)
        {
            _quantidadeDesbloqueadas++;
            _valorReferenciaDesbloqueio = Mathf.Max(1f, _maiorDinheiroAtingido);
            desbloqueou = true;
        }

        if (desbloqueou)
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
        GameDirector.instancia.saveManager?.Salvar();
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
        string textoBotao = ObterTextoLocalizado("upgrade.acquire", ObterFallbackAdquirir());

        if (item.textoTitulo != null)
            item.textoTitulo.text = titulo;
        if (item.textoDescricao != null)
            item.textoDescricao.text = descricao;
        if (item.textoValorAumentado != null)
            item.textoValorAumentado.text = valor;
        if (item.textoPreco != null)
            item.textoPreco.text = FormatarMoeda(melhoria.ObterPrecoAtual());
        if (item.textoBotao != null)
            item.textoBotao.text = textoBotao;
        if (item.botaoComprar != null && GameDirector.instancia != null && GameDirector.instancia.levelManenger != null)
            item.botaoComprar.interactable = GameDirector.instancia.levelManenger.dinheiro >= melhoria.ObterPrecoAtual();
    }

    private string ObterTextoLocalizado(string chave, string fallback, params object[] argumentos)
    {
        string resultado = null;

        try
        {
            LocalizedString localizedString = new LocalizedString(nomeTabelaLocalizacao, chave);
            resultado = argumentos != null && argumentos.Length > 0
                ? localizedString.GetLocalizedString(argumentos)
                : localizedString.GetLocalizedString();
        }
        catch
        {
            resultado = null;
        }

        if (string.IsNullOrWhiteSpace(resultado) || string.Equals(resultado, chave, StringComparison.OrdinalIgnoreCase))
            resultado = ObterTextoCsv(chave, argumentos);

        if (string.IsNullOrWhiteSpace(resultado))
            resultado = argumentos != null && argumentos.Length > 0 ? string.Format(fallback, argumentos) : fallback;

        return resultado;
    }

    private string ObterTextoCsv(string chave, params object[] argumentos)
    {
        CarregarLocalizacaoCsvSeNecessario();
        if (!_localizacaoCsv.TryGetValue(chave, out LinhaLocalizacaoCsv linha))
            return null;

        string codigoLocale = ObterCodigoLocaleAtual();
        string valor = null;

        if (!string.IsNullOrWhiteSpace(codigoLocale))
        {
            foreach (KeyValuePair<string, string> entrada in linha.valores)
            {
                if (entrada.Key.IndexOf($"({codigoLocale})", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    valor = entrada.Value;
                    break;
                }
            }

            if (valor == null && codigoLocale.Contains("-"))
            {
                string idioma = codigoLocale.Split('-')[0];
                foreach (KeyValuePair<string, string> entrada in linha.valores)
                {
                    if (entrada.Key.IndexOf($"({idioma}", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        valor = entrada.Value;
                        break;
                    }
                }
            }
        }

        if (string.IsNullOrWhiteSpace(valor))
        {
            foreach (KeyValuePair<string, string> entrada in linha.valores)
            {
                if (entrada.Key.IndexOf("(pt-BR)", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    valor = entrada.Value;
                    break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(valor))
            return null;

        return argumentos != null && argumentos.Length > 0 ? string.Format(valor, argumentos) : valor;
    }

    private string ObterCodigoLocaleAtual()
    {
        try
        {
            if (LocalizationSettings.SelectedLocale != null && LocalizationSettings.SelectedLocale.Identifier.Code != null)
                return LocalizationSettings.SelectedLocale.Identifier.Code;
        }
        catch
        {
        }

        return CultureInfo.CurrentUICulture?.Name;
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

    private string ObterFallbackAdquirir()
    {
        string codigoLocale = ObterCodigoLocaleAtual();
        return codigoLocale != null && codigoLocale.StartsWith("en", StringComparison.OrdinalIgnoreCase)
            ? "Acquire"
            : "Adquirir";
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
