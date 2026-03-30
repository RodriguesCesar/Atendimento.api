using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace Atendimento.API.Services
{
    public class OpenAIService
    {
        private readonly BaseDeConhecimentoService _baseDeConhecimentoService;
        private readonly ChatClient _chatClient;
        private readonly EmbeddingClient _embeddingClient;
        private readonly IConfiguration _configuration;

        private List<(string Text, float[] Vector)> _vectorDb = new();

        public OpenAIService(
            BaseDeConhecimentoService baseDeConhecimentoService,
            IConfiguration configuration)
        {
            _baseDeConhecimentoService = baseDeConhecimentoService;
            _configuration = configuration;

            var model = configuration["OpenAI:Model"]!;
            var key = configuration["OpenAI:Key"]!;

            var openAIClient = new OpenAIClient(key);

            // NOVO padrão
            _chatClient = openAIClient.GetChatClient(model);
            _embeddingClient = openAIClient.GetEmbeddingClient("text-embedding-3-small");

            LerBaseDeConhecimento().Wait();
        }

        /// <summary>
        /// Envia pergunta ao modelo
        /// </summary>
        public async Task<string> SendMessage(string text)
        {
            var contexto = await BuscarContexto(text);

            var prompt = MontarPrompt(text, contexto);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(prompt.System),
                new UserChatMessage(prompt.User)
            };

            var response = await _chatClient.CompleteChatAsync(messages);

            return response.Value.Content[0].Text;
        }

        /// <summary>
        /// Indexação (RAG)
        /// </summary>
        private async Task LerBaseDeConhecimento()
        {
            var docs = _baseDeConhecimentoService.LoadDocuments();

            foreach (var doc in docs)
            {
                foreach (var chunk in doc.Chunks)
                {
                    var emb = await _embeddingClient.GenerateEmbeddingAsync(chunk);

                    _vectorDb.Add((chunk, emb.Value.ToFloats().ToArray()));
                }
            }
        }

        /// <summary>
        /// Busca semântica
        /// </summary>
        private async Task<string> BuscarContexto(string pergunta)
        {
            var emb = await _embeddingClient.GenerateEmbeddingAsync(pergunta);

            var queryVector = emb.Value.ToFloats().ToArray();

            var top = _vectorDb
                .Select(x => new
                {
                    x.Text,
                    Score = CosineSimilarity(queryVector, x.Vector)
                })
                .OrderByDescending(x => x.Score)
                .Take(3)
                .Select(x => x.Text);

            return string.Join("\n\n", top);
        }

        /// <summary>
        /// Prompt Engineering
        /// </summary>
        private (string System, string User) MontarPrompt(string pergunta, string contexto)
        {
            var system = _configuration["EngenhariaDePrompt:AtendenteCOTI"]!;

            var user = $"""
            CONTEXTO:
            {contexto}

            PERGUNTA:
            {pergunta}
            """;

            return (system, user);
        }

        /// <summary>
        /// Similaridade de cosseno
        /// </summary>
        private double CosineSimilarity(float[] v1, float[] v2)
        {
            double dot = 0, mag1 = 0, mag2 = 0;

            for (int i = 0; i < v1.Length; i++)
            {
                dot += v1[i] * v2[i];
                mag1 += Math.Pow(v1[i], 2);
                mag2 += Math.Pow(v2[i], 2);
            }

            return dot / (Math.Sqrt(mag1) * Math.Sqrt(mag2));
        }
    }
}