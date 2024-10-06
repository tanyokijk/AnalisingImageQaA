using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using TranslateTextFromImage;

class Program
{
    private static readonly string endpoint = "https://computer-vision-dev-cus.cognitiveservices.azure.com/"; 
    private static readonly string subscriptionKey = "c5d19c144d534d8ea53637dc519027d0";

    static async Task Main(string[] args)
    {
        Console.WriteLine("Enter the path to the image:");
        string imagePath = Console.ReadLine();
        TextToSpeech textToSpeech = new TextToSpeech();
        var analysis = await AnalyzeImage(imagePath, textToSpeech);

        if (analysis != null)
        {

            while (true)
            {
                Console.WriteLine("Select a question by entering a number (1-6), or type 'exit' to quit:");
                ShowQuestions();

                string input = Console.ReadLine();
                if (input.ToLower() == "exit")
                {
                    break;
                }

                if (int.TryParse(input, out int questionNumber))
                {
                    string response = AnswerQuestions(analysis, questionNumber, textToSpeech);
                    Console.WriteLine(response);
                }
                else
                {
                    Console.WriteLine("Invalid input. Please try again.");
                    textToSpeech.SpeakAsync("Invalid input. Please try again.");
                }
            }
        }
        else
        {
            Console.WriteLine("Failed to analyze the image.");
        }
    }

    private static async Task<dynamic> AnalyzeImage(string imagePath, TextToSpeech textToSpeech)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            var uri = endpoint + "vision/v3.2/analyze?visualFeatures=Categories,Description,Objects";

            using (var stream = File.OpenRead(imagePath))
            {
                using (var content = new StreamContent(stream))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    var response = await client.PostAsync(uri, content);
                    string jsonResponse = await response.Content.ReadAsStringAsync();


                    var analysisResult = JsonConvert.DeserializeObject<dynamic>(jsonResponse);


                    if (analysisResult.objects != null && analysisResult.objects.Count > 0)
                    {
                        Console.WriteLine("Objects in the image:");
                        var objectsList = new List<string>(); 

                        foreach (var obj in analysisResult.objects)
                        {
                            var objectName = (string)obj["object"]; 
                            Console.WriteLine($"- {objectName}");
                            objectsList.Add(objectName); 
                        }


                        string objectsText = $"Objects in the image: {string.Join(", ", objectsList)}";
                        textToSpeech.SpeakAsync(objectsText);
                    }
                    else
                    {
                        Console.WriteLine("No objects detected in the image.");
                        textToSpeech.SpeakAsync("No objects detected in the image.");
                    }

                    return analysisResult;
                }
            
            }
        }
    }

    private static void ShowQuestions()
    {
        Console.WriteLine("1. How many people are in the image?");
        Console.WriteLine("2. What is this object?");
        Console.WriteLine("3. Describe the image.");
        Console.WriteLine("4. What objects are in the image?");
        Console.WriteLine("5. How many objects are in the image?");
        Console.WriteLine("6. Probabilities of objects in the image?");
    }

    private static string AnswerQuestions(dynamic analysis, int questionNumber, TextToSpeech textToSpeech)
    {
        string text = string.Empty;

        switch (questionNumber)
        {
            case 1: // How many people
                int countPeople = ((IEnumerable<dynamic>)analysis.objects).Count(obj => obj.objectProperty == "person");
                text = $"Number of people in the image: {countPeople}";
                textToSpeech.SpeakAsync(text);
                break;

            case 2: // What is this object
                var objectsList = ((IEnumerable<dynamic>)analysis.objects).Select(obj => (string)obj["object"]).ToList();
                if (objectsList.Count == 0)
                {
                    text = "There are no objects in the image.";
                    textToSpeech.SpeakAsync(text);
                }
                else
                {
                    text = $"Objects in the image: {string.Join(", ", objectsList)}";
                    textToSpeech.SpeakAsync(text);
                }
                textToSpeech.SpeakAsync(text);
                break;

            case 3: // Describe
                var captions = string.Join(", ", ((IEnumerable<dynamic>)analysis.description.captions).Select(c => c.text));
                text = $"Description of the image: {captions}";
                textToSpeech.SpeakAsync(text);
                break;

            case 4: // What objects
                var objectsList2 = ((IEnumerable<dynamic>)analysis.objects).Select(obj => (string)obj["object"]).ToList();
                if (objectsList2.Count == 0)
                {
                    text = "There are no objects in the image.";
                    textToSpeech.SpeakAsync(text);
                }
                else
                {
                    text = $"Objects in the image: {string.Join(", ", objectsList2)}";
                    textToSpeech.SpeakAsync(text);
                }
                textToSpeech.SpeakAsync(text);
                break;

            case 5: // How many objects
                int countObjects = ((IEnumerable<dynamic>)analysis.objects).Count();
                text = $"Number of objects in the image: {countObjects}";
                textToSpeech.SpeakAsync(text);
                break;

            case 6: // Probabilities
                var confidenceList = ((IEnumerable<dynamic>)analysis.objects)
                    .Select(obj => new { Type = (string)obj["object"], Confidence = (float)obj["confidence"] })
                    .ToList();

                if (confidenceList.Count == 0)
                {
                    text = "There are no objects in the image to assess probabilities.";
                    textToSpeech.SpeakAsync(text);
                }
                else
                {
                    var confidenceResults = string.Join(", ", confidenceList.Select(c => $"{c.Type}: {c.Confidence * 100}%"));
                    text = $"Probabilities of objects: {confidenceResults}";
                    textToSpeech.SpeakAsync(text);
                }
                break;

            default:
                text = "Question not understood. Please try again.";
                textToSpeech.SpeakAsync(text);
                break;
        }

        return text;
    }

}
