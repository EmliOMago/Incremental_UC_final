using UnityEngine;

public class SaveManagerPlayerPrefs : MonoBehaviour
{
    private const string ChaveEconomiaJogo = "economia_jogo_v1";
    private const string ChaveDinheiro = "dinheiro";
    private const string ChaveHudMelhorias = "hud_melhorias_v2";

    public bool ExisteSave()
    {
        return PlayerPrefs.HasKey(ChaveEconomiaJogo)
            || PlayerPrefs.HasKey(ChaveDinheiro)
            || PlayerPrefs.HasKey(ChaveHudMelhorias);
    }

    public DadosEconomiaJogo ObterDadosEconomiaAtual()
    {
        if (GameDirector.instancia == null || GameDirector.instancia.levelManenger == null)
            return null;

        if (GameDirector.instancia.hudManeger != null)
            return GameDirector.instancia.hudManeger.CriarDadosEconomia(GameDirector.instancia.levelManenger.dinheiro);

        return new DadosEconomiaJogo
        {
            dinheiroAtual = Mathf.Max(0f, GameDirector.instancia.levelManenger.dinheiro),
            totalComprasMelhorias = 0,
            quantidadeMelhoriasDesbloqueadas = 0,
            valorReferenciaDesbloqueio = 1f,
            maiorDinheiroAtingido = Mathf.Max(0f, GameDirector.instancia.levelManenger.dinheiro)
        };
    }

    public string ExportarDadosEconomiaJson()
    {
        DadosEconomiaJogo dados = ObterDadosEconomiaAtual();
        return dados != null ? JsonUtility.ToJson(dados) : string.Empty;
    }

    public void Salvar()
    {
        if (GameDirector.instancia == null || GameDirector.instancia.levelManenger == null)
            return;

        DadosEconomiaJogo dados = ObterDadosEconomiaAtual();
        float dinheiroAtual = dados != null ? dados.dinheiroAtual : Mathf.Max(0f, GameDirector.instancia.levelManenger.dinheiro);

        PlayerPrefs.SetFloat(ChaveDinheiro, dinheiroAtual);
        PlayerPrefs.SetString(ChaveEconomiaJogo, dados != null ? JsonUtility.ToJson(dados) : string.Empty);

        if (GameDirector.instancia.hudManeger != null)
            PlayerPrefs.SetString(ChaveHudMelhorias, GameDirector.instancia.hudManeger.ExportarSaveHUD());

        PlayerPrefs.Save();
    }

    public void CarregarOuCriarNovoSave(bool atualizarHUD = true)
    {
        if (ExisteSave())
        {
            bool carregou = Carregar(atualizarHUD);
            if (carregou)
                return;
        }

        CriarNovoSave(atualizarHUD);
    }

    public bool Carregar(bool atualizarHUD = true)
    {
        if (GameDirector.instancia == null || GameDirector.instancia.levelManenger == null)
            return false;

        bool existeAlgumSave = ExisteSave();
        if (!existeAlgumSave)
            return false;

        string jsonEconomia = PlayerPrefs.GetString(ChaveEconomiaJogo, string.Empty);
        if (!string.IsNullOrWhiteSpace(jsonEconomia))
        {
            DadosEconomiaJogo dados = JsonUtility.FromJson<DadosEconomiaJogo>(jsonEconomia);
            if (dados != null)
            {
                GameDirector.instancia.levelManenger.dinheiro = Mathf.Max(0f, dados.dinheiroAtual);

                if (GameDirector.instancia.hudManeger != null)
                    GameDirector.instancia.hudManeger.AplicarDadosEconomia(dados, atualizarHUD);

                if (atualizarHUD && GameDirector.instancia.hudManeger != null)
                    GameDirector.instancia.hudManeger.AtualizarTudo();

                return true;
            }
        }

        GameDirector.instancia.levelManenger.dinheiro = PlayerPrefs.GetFloat(ChaveDinheiro, 0f);

        if (GameDirector.instancia.hudManeger != null)
        {
            string jsonHud = PlayerPrefs.GetString(ChaveHudMelhorias, string.Empty);
            GameDirector.instancia.hudManeger.AplicarSaveHUD(jsonHud, atualizarHUD);

            if (atualizarHUD)
                GameDirector.instancia.hudManeger.AtualizarTudo();
        }

        return true;
    }

    private void CriarNovoSave(bool atualizarHUD = true)
    {
        if (GameDirector.instancia == null || GameDirector.instancia.levelManenger == null)
            return;

        GameDirector.instancia.levelManenger.dinheiro = 0f;

        if (GameDirector.instancia.hudManeger != null)
            GameDirector.instancia.hudManeger.AplicarDadosEconomia(null, atualizarHUD);

        Salvar();

        if (atualizarHUD && GameDirector.instancia.hudManeger != null)
            GameDirector.instancia.hudManeger.AtualizarTudo();
    }
}
