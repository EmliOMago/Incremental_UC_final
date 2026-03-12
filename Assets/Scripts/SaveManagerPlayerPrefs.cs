using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManagerPlayerPrefs : MonoBehaviour
{
    private const string ChaveCriptografiaArquivos = "EmliOMago";
    private const string NomeArquivoJson = "estado_atual_jogo.json";
    private const string NomeArquivoCsv = "estado_atual_jogo.csv";

    [Header("Auto save")]
    [Tooltip("Intervalo, em segundos, entre tentativas de auto save.")]
    [Min(0.25f)]
    public float intervaloAutoSaveSegundos = 5f;

    [Tooltip("Se verdadeiro, o jogo grava os arquivos imediatamente após criar um save zerado.")]
    public bool salvarImediatamenteAoCriarNovoSave = true;

    private bool _carregamentoInicialSolicitado;
    private bool _savePendente;
    private string _ultimoJsonSalvo;
    private string _ultimoCsvSalvo;

    public bool CarregamentoInicialConcluido { get; private set; }
    public float IntervaloAutoSaveSegundos => Mathf.Max(0.25f, intervaloAutoSaveSegundos);

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "CenaJogo")
            SolicitarCarregamentoInicial();
    }

    public void SolicitarCarregamentoInicial()
    {
        if (_carregamentoInicialSolicitado)
            return;

        _carregamentoInicialSolicitado = true;
        CarregamentoInicialConcluido = false;
        CarregarOuCriar();
        CarregamentoInicialConcluido = true;
    }

    public void Salvar()
    {
        Salvar(forcar: true);
    }

    public bool SalvarSeNecessario()
    {
        return Salvar(forcar: false);
    }

    public void MarcarSaveComoSujo()
    {
        _savePendente = true;
    }

    private bool Salvar(bool forcar)
    {
        if (!forcar && !_savePendente)
            return false;

        DadosEconomiaJogo dados = CapturarDadosEconomia();
        if (dados == null)
            return false;

        bool escreveu = SalvarDadosNosArquivos(dados, forcar);
        if (escreveu)
            _savePendente = false;

        return escreveu;
    }

    public void Carregar()
    {
        DadosEconomiaJogo dados = LerDadosDosArquivos();
        if (dados == null)
            return;

        AplicarDadosAoJogo(dados);
        ExportarArquivosEstadoAtual();
        _savePendente = false;
    }

    public void CarregarOuCriar()
    {
        DadosEconomiaJogo dados = LerDadosDosArquivos();
        if (dados == null)
        {
            dados = CriarEstadoZerado();
            if (salvarImediatamenteAoCriarNovoSave)
                SalvarDadosNosArquivos(dados, true);
        }

        AplicarDadosAoJogo(dados);
        ExportarArquivosEstadoAtual();
        _savePendente = false;
    }

    public void LimparSaveCompleto()
    {
        LimparSaveGlobal();

        DadosEconomiaJogo dados = CriarEstadoZerado();
        AplicarDadosAoJogo(dados);
        SalvarDadosNosArquivos(dados, true);
        _savePendente = false;
    }

    public static void LimparSaveGlobal()
    {
        ExcluirArquivosEstadoAtual();
    }

    public void ExportarArquivosEstadoAtual()
    {
        DadosEconomiaJogo dados = CapturarDadosEconomia();
        if (dados == null)
            return;

        ExportarArquivosEstadoAtual(dados, true);
    }

    private bool ExportarArquivosEstadoAtual(DadosEconomiaJogo dados, bool forcarEscrita)
    {
        string diretorio = ObterDiretorioDaAplicacao();
        if (string.IsNullOrWhiteSpace(diretorio))
            return false;

        Directory.CreateDirectory(diretorio);

        string caminhoJson = Path.Combine(diretorio, NomeArquivoJson);
        string caminhoCsv = Path.Combine(diretorio, NomeArquivoCsv);

        string json = JsonUtility.ToJson(dados, true);
        string csv = ConverterDadosParaCsv(dados);

        bool mudouJson = !string.Equals(_ultimoJsonSalvo, json, StringComparison.Ordinal);
        bool mudouCsv = !string.Equals(_ultimoCsvSalvo, csv, StringComparison.Ordinal);

        if (!forcarEscrita && !mudouJson && !mudouCsv)
            return false;

        if (forcarEscrita || mudouJson)
        {
            File.WriteAllText(caminhoJson, AplicarCriptografiaSeNecessario(json), Encoding.UTF8);
            _ultimoJsonSalvo = json;
        }

        if (forcarEscrita || mudouCsv)
        {
            File.WriteAllText(caminhoCsv, AplicarCriptografiaSeNecessario(csv), Encoding.UTF8);
            _ultimoCsvSalvo = csv;
        }

        return forcarEscrita || mudouJson || mudouCsv;
    }

    private bool SalvarDadosNosArquivos(DadosEconomiaJogo dados, bool forcarEscrita)
    {
        if (dados == null)
            return false;

        return ExportarArquivosEstadoAtual(dados, forcarEscrita);
    }

    private DadosEconomiaJogo LerDadosDosArquivos()
    {
        string diretorio = ObterDiretorioDaAplicacao();
        if (string.IsNullOrWhiteSpace(diretorio) || !Directory.Exists(diretorio))
            return null;

        string caminhoJson = Path.Combine(diretorio, NomeArquivoJson);
        if (!File.Exists(caminhoJson))
            return null;

        try
        {
            string conteudo = File.ReadAllText(caminhoJson, Encoding.UTF8);
            conteudo = RemoverCriptografiaSeNecessario(conteudo);
            if (string.IsNullOrWhiteSpace(conteudo))
                return null;

            DadosEconomiaJogo dados = JsonUtility.FromJson<DadosEconomiaJogo>(conteudo);
            _ultimoJsonSalvo = JsonUtility.ToJson(dados, true);
            _ultimoCsvSalvo = ConverterDadosParaCsv(dados);
            return dados;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Falha ao ler save JSON em '{caminhoJson}': {ex.Message}");
            return null;
        }
    }

    private DadosEconomiaJogo CriarEstadoZerado()
    {
        GameDirector.instancia?.AtualizarReferenciasDaCena();

        if (GameDirector.instancia != null && GameDirector.instancia.levelManenger != null)
            GameDirector.instancia.levelManenger.dinheiro = 0f;

        return new DadosEconomiaJogo
        {
            dinheiroAtual = 0f,
            totalComprasMelhorias = 0,
            quantidadeMelhoriasDesbloqueadas = 0,
            valorReferenciaDesbloqueio = 0f,
            maiorDinheiroAtingido = 0f
        };
    }

    private void AplicarDadosAoJogo(DadosEconomiaJogo dados)
    {
        GameDirector.instancia?.AtualizarReferenciasDaCena();

        if (GameDirector.instancia != null && GameDirector.instancia.levelManenger != null)
            GameDirector.instancia.levelManenger.dinheiro = Mathf.Max(0f, dados != null ? dados.dinheiroAtual : 0f);

        if (GameDirector.instancia != null && GameDirector.instancia.hudManeger != null)
            GameDirector.instancia.hudManeger.AplicarDadosEconomiaJogo(dados);
    }

    private DadosEconomiaJogo CapturarDadosEconomia()
    {
        GameDirector.instancia?.AtualizarReferenciasDaCena();

        DadosEconomiaJogo dadosHud = GameDirector.instancia != null && GameDirector.instancia.hudManeger != null
            ? GameDirector.instancia.hudManeger.CapturarDadosEconomiaJogo()
            : null;

        if (dadosHud != null)
            return dadosHud;

        DadosEconomiaJogo dados = new DadosEconomiaJogo();
        dados.dinheiroAtual = GameDirector.instancia != null && GameDirector.instancia.levelManenger != null
            ? GameDirector.instancia.levelManenger.dinheiro
            : 0f;
        return dados;
    }

    private static string ObterDiretorioDaAplicacao()
    {
        return Application.persistentDataPath;
    }

    private static void ExcluirArquivosEstadoAtual()
    {
        string diretorio = ObterDiretorioDaAplicacao();
        if (string.IsNullOrWhiteSpace(diretorio) || !Directory.Exists(diretorio))
            return;

        string caminhoJson = Path.Combine(diretorio, NomeArquivoJson);
        string caminhoCsv = Path.Combine(diretorio, NomeArquivoCsv);

        if (File.Exists(caminhoJson))
            File.Delete(caminhoJson);

        if (File.Exists(caminhoCsv))
            File.Delete(caminhoCsv);
    }

    private static string ConverterDadosParaCsv(DadosEconomiaJogo dados)
    {
        StringBuilder sb = new StringBuilder(1024);
        sb.AppendLine("Secao,Campo,Valor");
        sb.AppendLine("Resumo,dataUtc," + EscaparCsv(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)));
        sb.AppendLine("Resumo,dinheiroAtual," + FormatarNumero(dados.dinheiroAtual));
        sb.AppendLine("Resumo,totalComprasMelhorias," + dados.totalComprasMelhorias.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine("Resumo,quantidadeMelhoriasDesbloqueadas," + dados.quantidadeMelhoriasDesbloqueadas.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine("Resumo,valorReferenciaDesbloqueio," + FormatarNumero(dados.valorReferenciaDesbloqueio));
        sb.AppendLine("Resumo,maiorDinheiroAtingido," + FormatarNumero(dados.maiorDinheiroAtingido));
        sb.AppendLine();
        sb.AppendLine("Melhorias,indiceTabela,idMelhoria,categoria,quantidadeComprada,precoBase,precoAtual,multiplicadorPreco,percentualPorCompra,multiplicadorTotal");

        if (dados.melhorias != null)
        {
            for (int i = 0; i < dados.melhorias.Count; i++)
            {
                DadosMelhoriaEconomia melhoria = dados.melhorias[i];
                if (melhoria == null)
                    continue;

                sb.Append("Melhoria").Append(',');
                sb.Append(melhoria.indiceTabela.ToString(CultureInfo.InvariantCulture)).Append(',');
                sb.Append(EscaparCsv(melhoria.idMelhoria)).Append(',');
                sb.Append(EscaparCsv(melhoria.categoria)).Append(',');
                sb.Append(melhoria.quantidadeComprada.ToString(CultureInfo.InvariantCulture)).Append(',');
                sb.Append(FormatarNumero(melhoria.precoBase)).Append(',');
                sb.Append(FormatarNumero(melhoria.precoAtual)).Append(',');
                sb.Append(FormatarNumero(melhoria.multiplicadorPreco)).Append(',');
                sb.Append(FormatarNumero(melhoria.percentualPorCompra)).Append(',');
                sb.Append(FormatarNumero(melhoria.multiplicadorTotal));
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static string FormatarNumero(float valor)
    {
        return valor.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private static string EscaparCsv(string valor)
    {
        if (string.IsNullOrEmpty(valor))
            return string.Empty;

        bool precisaAspas = valor.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
        if (!precisaAspas)
            return valor;

        return "\"" + valor.Replace("\"", "\"\"") + "\"";
    }

    private static string AplicarCriptografiaSeNecessario(string conteudo)
    {
#if UNITY_EDITOR
        return conteudo;
#else
        return CriptografarTexto(conteudo, ChaveCriptografiaArquivos);
#endif
    }

    private static string RemoverCriptografiaSeNecessario(string conteudo)
    {
#if UNITY_EDITOR
        return conteudo;
#else
        return DescriptografarTexto(conteudo, ChaveCriptografiaArquivos);
#endif
    }

    private static string CriptografarTexto(string texto, string chave)
    {
        byte[] dados = Encoding.UTF8.GetBytes(texto ?? string.Empty);
        byte[] chaveBytes;

        using (SHA256 sha = SHA256.Create())
            chaveBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(chave ?? string.Empty));

        using (Aes aes = Aes.Create())
        {
            aes.Key = chaveBytes;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            {
                byte[] criptografado = encryptor.TransformFinalBlock(dados, 0, dados.Length);
                byte[] combinado = new byte[aes.IV.Length + criptografado.Length];
                Buffer.BlockCopy(aes.IV, 0, combinado, 0, aes.IV.Length);
                Buffer.BlockCopy(criptografado, 0, combinado, aes.IV.Length, criptografado.Length);
                return Convert.ToBase64String(combinado);
            }
        }
    }

    private static string DescriptografarTexto(string textoCriptografado, string chave)
    {
        byte[] combinado = Convert.FromBase64String(textoCriptografado ?? string.Empty);
        byte[] chaveBytes;

        using (SHA256 sha = SHA256.Create())
            chaveBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(chave ?? string.Empty));

        using (Aes aes = Aes.Create())
        {
            aes.Key = chaveBytes;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            byte[] iv = new byte[aes.BlockSize / 8];
            byte[] dados = new byte[combinado.Length - iv.Length];
            Buffer.BlockCopy(combinado, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(combinado, iv.Length, dados, 0, dados.Length);
            aes.IV = iv;

            using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            {
                byte[] descriptografado = decryptor.TransformFinalBlock(dados, 0, dados.Length);
                return Encoding.UTF8.GetString(descriptografado);
            }
        }
    }
}
