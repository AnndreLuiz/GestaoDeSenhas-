using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Timers;
using PasswordManagementAPI.Models;

namespace PasswordManagementAPI.Controllers
{
    [ApiController]
    [Route("api/senhas")]
    public class SenhaController : ControllerBase
    {
        private static List<Senha> _senhas = new List<Senha>();
        private readonly HttpClient _httpClient;
        private readonly ILogger<SenhaController> _logger;
        private readonly IConfiguration _config;
        private readonly System.Timers.Timer _timer;

        public SenhaController(HttpClient httpClient, ILogger<SenhaController> logger, IConfiguration config)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = config;

            _timer = new System.Timers.Timer();
            _timer.Interval = _config.GetValue<int>("Configuracoes:TempoExpiracao") * 60000;
            _timer.Elapsed += CancelarSenhasExpiradas;
            _timer.Start();
        }

        [HttpPost("emitir")]
        public async Task<IActionResult> EmitirSenha()
        {
            var senha = new Senha();
            var cotacoes = await ObterCotacaoMoedas();
            senha.CotacaoDolar = cotacoes.Dolar;
            senha.CotacaoEuro = cotacoes.Euro;

            _senhas.Add(senha);
            RegistrarLog($"Senha {senha.Codigo} emitida.");

            return Ok(senha);
        }

        [HttpPost("emitir-ap")]
        public async Task<IActionResult> EmitirSenhaAP()
        {
            var senha = new Senha("AP");
            var cotacoes = await ObterCotacaoMoedas();
            senha.CotacaoDolar = cotacoes.Dolar;
            senha.CotacaoEuro = cotacoes.Euro;

            _senhas.Add(senha);
            RegistrarLog($"Senha {senha.Codigo} emitida (AP).");

            return Ok(senha);
        }

        [HttpPost("cancelar/{codigo}")]
        public IActionResult CancelarSenha(string codigo)
        {
            var senha = _senhas.FirstOrDefault(s => s.Codigo == codigo);
            if (senha == null || senha.Status != "Ativa")
            {
                return NotFound("Senha não encontrada ou já finalizada/cancelada.");
            }

            senha.Status = "Cancelada";
            RegistrarLog($"Senha {codigo} cancelada.");
            return Ok(senha);
        }

        [HttpPost("finalizar/{codigo}")]
        public IActionResult FinalizarSenha(string codigo)
        {
            var senha = _senhas.FirstOrDefault(s => s.Codigo == codigo);
            if (senha == null || senha.Status != "Ativa")
            {
                return NotFound("Senha não encontrada ou já finalizada/cancelada.");
            }

            senha.Status = "Finalizada";
            RegistrarLog($"Senha {codigo} finalizada.");
            return Ok(senha);
        }

        [HttpGet("listar/{status}")]
        public IActionResult ListarSenhas(string status)
        {
            var senhasFiltradas = _senhas.Where(s => s.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            return Ok(senhasFiltradas);
        }

        private async Task<(string Dolar, string Euro)> ObterCotacaoMoedas()
        {
            try
            {
                var response = await _httpClient.GetStringAsync("https://economia.awesomeapi.com.br/json/last/USD-BRL,EUR-BRL");
                var json = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(response);
                string dolar = json?["USDBRL"]["bid"] ?? "0.00";
                string euro = json?["EURBRL"]["bid"] ?? "0.00";
                return (dolar, euro);
            }
            catch
            {
                return ("0.00", "0.00");
            }
        }

        private void CancelarSenhasExpiradas(object? sender, ElapsedEventArgs e)
        {
            var agora = DateTime.UtcNow;
            var senhasExpiradas = _senhas.Where(s => s.Status == "Ativa" && (agora - s.CriadaEm) > TimeSpan.FromMinutes(_config.GetValue<int>("Configuracoes:TempoExpiracao"))).ToList();

            foreach (var senha in senhasExpiradas)
            {
                senha.Status = "Cancelada";
                RegistrarLog($"Senha {senha.Codigo} cancelada automaticamente após {_config.GetValue<int>("Configuracoes:TempoExpiracao")} minutos.");
            }
        }

        private void RegistrarLog(string mensagem)
        {
            // Usando Serilog para registrar os logs
            _logger.LogInformation(mensagem);
        }
    }

}
