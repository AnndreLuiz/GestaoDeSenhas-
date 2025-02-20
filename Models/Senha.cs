namespace PasswordManagementAPI.Models;

public class Senha
{
    private static int _contador = 1;
    public string Codigo { get; private set; }
    public string Status { get; set; } = "Ativa";
    public DateTime CriadaEm { get; private set; } = DateTime.Now;
    public string CotacaoDolar { get; set; } = "0.00";
    public string CotacaoEuro { get; set; } = "0.00";

    public Senha(string prefixo = "AN")
    {
        Codigo = $"{prefixo}{_contador:D2}";
        _contador++;
    }
}