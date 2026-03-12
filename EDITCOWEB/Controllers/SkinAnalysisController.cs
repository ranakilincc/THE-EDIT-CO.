using System.Text.Json;
using EDITCOWEB.Models;
using Microsoft.AspNetCore.Mvc;

namespace EDITCOWEB.Controllers
{
    public class SkinAnalysisController : Controller
    {
        private readonly IConfiguration _configuration;

        public SkinAnalysisController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
            {
                ViewBag.Error = "Lütfen bir fotoğraf seçin.";
                return View();
            }

            string baseUrl = _configuration["FastApiSettings:BaseUrl"];
            string apiUrl = $"{baseUrl}/analyze";

            using var httpClient = new HttpClient();
            using var formData = new MultipartFormDataContent();

            using var stream = photo.OpenReadStream();
            using var streamContent = new StreamContent(stream);

            streamContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(photo.ContentType);

            formData.Add(streamContent, "file", photo.FileName);

            HttpResponseMessage response;

            try
            {
                response = await httpClient.PostAsync(apiUrl, formData);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "FastAPI servisine bağlanılamadı. Hata: " + ex.Message;
                return View();
            }

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                ViewBag.Error = $"Analiz servisi hata döndürdü. Status: {(int)response.StatusCode} - {response.ReasonPhrase}. Detay: {errorBody}";
                return View();
            }

            string json = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            SkinAnalysisResult? result =
                JsonSerializer.Deserialize<SkinAnalysisResult>(json, options);

            if (result == null)
            {
                ViewBag.Error = "Analiz sonucu okunamadı.";
                return View();
            }

            return View("Result", result);
        }
    }
}