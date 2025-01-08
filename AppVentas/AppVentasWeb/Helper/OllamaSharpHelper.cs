﻿using OllamaSharp;
using System;

namespace AppVentasWeb.Helper
{
    public class OllamaSharpHelper : IOllamaSharpHelper
    {
        private readonly Uri _endpoint;
        private readonly string _model;

        public OllamaSharpHelper(IConfiguration configuration)
        {
            _endpoint = new Uri(configuration["LlamaAI_Parametros:endpoint"]);
            _model = configuration["LlamaAI_Parametros:modelo"];
        }
        public OllamaApiClient GetChatOllamaApiClient()
        {

            OllamaApiClient ollama = new OllamaApiClient(_endpoint);
            ollama.SelectedModel = _model;

            return ollama;
        }

        public async Task<string> GetRespuestaOllamaAsync(string userMensaje, string schemaBd)
        {

            string msjSystem= "Eres un generador de consulats SQL de SqlServer. Debes devolver solo T-SQL, sin ninguna explicacion adicional, para las siguientes Tablas: "
                + schemaBd + ". NO debes generar querys que realizen cambios en la base de datos del tipo: INSERT,UPDATE,DELETE,DROP. La respuesta NO debe incluir caracteres como ```sql";

            var ollama = GetChatOllamaApiClient();
            var chat = new Chat(ollama, msjSystem);


            string respuesta = await chat.SendAsync(userMensaje).StreamToEndAsync();

            if (respuesta.Contains("```") || respuesta.Contains("sql"))
            {
                respuesta = respuesta.Replace("```", string.Empty).Replace("sql", string.Empty);
            }

            return respuesta;
        }

        public async Task<string> GetRespuestaOllamaFinalAsync(string userMensaje, string datosbd)
        {


            string msjSystem = "Eres un agente IA experto en información de la empresa, dedicado a ayudar amablemente a los usuarios. Proporcionas respuestas precisas y útiles, y tu enfoque principal es brindar una experiencia amigable y eficiente. debes responder basado en: "+ datosbd;

            var ollama = GetChatOllamaApiClient();
            var chat = new Chat(ollama, msjSystem);

            string respuesta2 = await chat.SendAsync(userMensaje).StreamToEndAsync();

            return respuesta2;


        }
    }
}
