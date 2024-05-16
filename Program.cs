using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace ExtrairPDFsDeChamados
{
    class Program
    {

        static async Task Autenticar(HttpClient httpClient, string username, string password)
        {
            // URL de login
            string loginUrl = "https://arquivo.atg.com.br";

            // Criar os parâmetros de login
            var parametros = new FormUrlEncodedContent(new[])
            {
                new KeyValuePair<string, string>("login_name", username),
                new KeyValuePair<string, string>("login_password", password),
                new KeyValuePair<string, string>("submit", "Entrar")
            });

            // Enviar a solicitação POST de login
            var resposta = await httpClient.PostAsync(loginUrl, parametros);
            resposta.EnsureSuccessStatusCode(); // Lança uma exceção se a solicitação falhar

            // Verificar se o login foi bem-sucedido
            string responseBody = await resposta.Content.ReadAsStringAsync();
            if (responseBody.Contains("Uso inválido de ID de sessão"))
            {
                throw new Exception("Falha no login: Uso inválido de ID de sessão.");
            }
        }

        static async Task Main(string[] args)
        {
            // URL base para os chamados
            string baseUrl = "https://arquivo.atg.com.br/front/ticket.form.php?id=";

            // IDs dos chamados
            int[] chamados = { 9255 }; // Adicione os IDs dos seus chamados aqui

            // Diretório para salvar os PDFs
            string saveDirectory = @"C:\"; // Altere para o diretório desejado

            // Credenciais de login
            string username = "lucas.silva";
            string password = "abc123";

            // Criar HttpClient
            HttpClientHandler handler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                UseCookies = true,
            };
            HttpClient httpClient = new HttpClient(handler);

            // Autenticar
            await Autenticar(httpClient, username, password);

            foreach (int chamadoId in chamados)
            {
                // Construir URL do chamado
                string url = baseUrl + chamadoId;

                // Obter o HTML da página do chamado
                string html = await httpClient.GetStringAsync(url);

                // Analisar o HTML para encontrar o link do PDF
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                // Encontrar o link do PDF
                HtmlNode linkNode = htmlDocument.DocumentNode.SelectSingleNode("//a[contains(@href, '/front/document.send.php')]");
                if (linkNode != null)
                {
                    // Extrair o link do PDF
                    string pdfUrl = "https://arquivo.atg.com.br" + linkNode.GetAttributeValue("href", "");

                    // Baixar o PDF
                    byte[] pdfBytes = await httpClient.GetByteArrayAsync(pdfUrl);

                    // Salvar o PDF no diretório especificado
                    string fileName = $"chamado_{chamadoId}.pdf";
                    string filePath = Path.Combine(saveDirectory, fileName);
                    File.WriteAllBytes(filePath, pdfBytes);

                    Console.WriteLine($"PDF do chamado {chamadoId} salvo em: {filePath}");
                }
                else
                {
                    Console.WriteLine($"Não foi possível encontrar o PDF para o chamado {chamadoId}");
                }
            }

            httpClient.Dispose();
        }
    }
}